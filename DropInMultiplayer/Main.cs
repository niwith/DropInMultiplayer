using BepInEx;
using BepInEx.Logging;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace DropInMultiplayer
{
    [BepInPlugin(guid, modName, version)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    public class DropInMultiplayer : BaseUnityPlugin
    {
        const string guid = "com.niwith.DropInMultiplayer";
        const string modName = "Drop In Multiplayer";
        const string version = "1.0.18";

        public DropInMultiplayerConfig DropInConfig { get; private set; }
        public static DropInMultiplayer Instance { get; private set; }


        private readonly Vector3 _spawnOffset = new Vector3(0, 1, 0);

        /// <summary>
        /// Temporary control to lock or unlock drop in, does not alter config setting
        /// </summary>
        public bool JoinAsBlocked => _blockingReasons.Any();
        public readonly Dictionary<Guid, string> _blockingReasons = new Dictionary<Guid, string>();

        /// <summary>
        /// Adds a new reason to block join_as command. Save the guid somewhere, or your mod will not be able to
        /// unblock the join_as.
        /// </summary>
        /// <param name="reason"></param>
        /// <returns></returns>
        public Guid BlockJoinAs(string reason)
        {
            var id = Guid.NewGuid();
            Logger.LogInfo($"Adding JoinAs Blocker with token {id} and blocker reason {reason}");
            _blockingReasons.Add(id, reason);
            return id;
        }

        /// <summary>
        /// Removes the blocker specified by the token from the blocking reasons, if all blockers are removed then 
        /// join_as will be enabled
        /// </summary>
        /// <param name="token"></param>
        public void UnBlockJoinAs(Guid token)
        {
            Logger.LogInfo($"Removing JoinAs Blocker with token {token} and blocker reason {_blockingReasons[token]}");
            _blockingReasons.Remove(token);
        }

        /// <summary>
        /// Removes all join as blockers, don't use this unless you know what you are doing, other mods may have set blockers for a good reason
        /// </summary>
        public void ClearJoinAsBlockers()
        {
            _blockingReasons.Clear();
        }

        /// <summary>
        /// No longer required, items to be dropped are now pulled dynamically from run instance.
        /// 
        /// Adds the given items to the list of items which should not be dropped to a player when joining
        /// </summary>
        /// <param name="items">Items to add to the invalid list</param>
        /// <param name="alsoInvalidForCount">When set to true will also add the items to the list of items to ingore
        /// when counting the number of items a player has (e.g. by default the captain's defense matrix doesn't count 
        /// towards if a newly joining captain gets a new red item nor does it count towards the average number of
        /// red items for another newly joining player)</param>
        [Obsolete("No longer required, items to be dropped are now pulled dynamically from run instance")]
        public static void AddInvalidItems(IEnumerable<ItemIndex> items, bool alsoInvalidForCount = false)
        {
            ItemsHelper.AddInvalidItems(items, alsoInvalidForCount);
        }

        public void Awake()
        {
            Instance = this;
            DropInConfig = new DropInMultiplayerConfig(base.Config);
            SetupHooks();
            CommandHelper.AddToConsoleWhenReady();
        }

        [ConCommand(commandName = "dim_logdropitemstofile", flags = ConVarFlags.None, helpText = "Writes currently availible drop items which can be blacklisted to the given folder path (defaults to your documents in folder named \"RiskOfRain2Items\"")]
        private static void MyCommandName(ConCommandArgs args)
        {
            if (args.Count == 0)
            {
                var directory = FileHelper.LogDropItemsToFile();
                Debug.Log("Wrote files to directory: " + directory);
            }
            else if (args.Count == 1)
            {
                var folderPath = args[0];
                var directory = FileHelper.LogDropItemsToFile(folderPath);
                Debug.Log("Wrote files to directory: " + directory);
            }
            else
            {
                Debug.Log("dim_logdropitemstofile requires 0 or 1 argument");
            }
        }

        private void SetupHooks()
        {
            On.RoR2.Console.RunCmd += CheckChatForJoinRequest;
            On.RoR2.NetworkUser.Start += GreetNewPlayer;
            On.RoR2.Run.SetupUserCharacterMaster += GiveItems;
            On.RoR2.Run.OnServerSceneChanged += CheckStageForBlockJoinAs;

#if DEBUG
            Logger.LogWarning("You're on a debug build. If you see this after downloading from the thunderstore, panic!");
            //This is so we can connect to ourselves.
            //Instructions:
            //Step One: Assuming this line is in your codebase, start two instances of RoR2 (do this through the .exe directly)
            //Step Two: Host a game with one instance of RoR2.
            //Step Three: On the instance that isn't hosting, open up the console (ctrl + alt + tilde) and enter the command "connect localhost:7777"
            //DO NOT MAKE A MISTAKE SPELLING THE COMMAND OR YOU WILL HAVE TO RESTART THE CLIENT INSTANCE!!
            //Step Four: Test whatever you were going to test.
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
#endif
        }

        private Guid _blockingJoinForFinalStageToken = Guid.Empty;
        private void CheckStageForBlockJoinAs(On.RoR2.Run.orig_OnServerSceneChanged orig, Run self, string sceneName)
        {
            orig(self, sceneName);

            if (!DropInConfig.AllowMoonDropIn)
            {
                if ((sceneName.Equals("moon") || sceneName.Equals("moon2")) && _blockingJoinForFinalStageToken.Equals(Guid.Empty))
                {
                    _blockingJoinForFinalStageToken = BlockJoinAs("Cannot join on final stage, may softlock run");
                }
                else if (!_blockingJoinForFinalStageToken.Equals(Guid.Empty))
                {
                    UnBlockJoinAs(_blockingJoinForFinalStageToken);
                    _blockingJoinForFinalStageToken = Guid.Empty;
                }
            }
        }

        private void CheckChatForJoinRequest(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            orig(self, sender, concommandName, userArgs);

            if (concommandName.Equals("say", StringComparison.InvariantCultureIgnoreCase))
            {
                var userInput = userArgs.FirstOrDefault().Split(new char[] { ' ' }, count: 3);
                var chatCommand = userInput.FirstOrDefault();
                if (chatCommand.IsNullOrWhiteSpace())
                {
                    return;
                }

                if (chatCommand.Equals("join_as", StringComparison.InvariantCultureIgnoreCase) || chatCommand.Equals("join", StringComparison.InvariantCultureIgnoreCase))
                {
                    var bodyString = userInput.ElementAtOrDefault(1) ?? "";
                    var userString = userInput.ElementAtOrDefault(2) ?? "";

                    JoinAs(sender.networkUser, bodyString, userString);
                }
            }
        }

        private void GreetNewPlayer(On.RoR2.NetworkUser.orig_Start orig, NetworkUser self)
        {
            orig(self);
            if (NetworkServer.active && Stage.instance != null && //Make sure we're host.
                DropInConfig.WelcomeMessage) //If the host man has enabled this config option.
            {
                var message = DropInConfig.CustomWelcomeMessage;
                if (message.Length > 1000)
                {
                    return;
                }
                message = message.ReplaceOnce("{username}", self.userName);
                message = message.ReplaceOnce("{survivorlist}", string.Join(", ", BodyHelper.GetSurvivorDisplayNames()));
                AddChatMessage(message, 1f);
            }
        }

        [Server]
        private void GiveItems(On.RoR2.Run.orig_SetupUserCharacterMaster orig, Run run, NetworkUser user)
        {
            orig(run, user);

            if (!DropInConfig.StartWithItems ||
                !run.isServer || // If we are not the server don't try to give items, let the server handle it
                run.fixedTime < 5f) // Don't try to give items to players who spawn with the server
            {
                return;
            }

            if (DropInConfig.GiveExactItems)
            {
                ItemsHelper.CopyItemsFromRandom(user);
            }
            else
            {
                ItemsHelper.GiveAveragedItems(user, DropInConfig.GiveRedItems, DropInConfig.GiveLunarItems, DropInConfig.GiveBossItems);
            }
        }
        
        private void ChangeOrSetCharacter(NetworkUser player, GameObject bodyPrefab, bool firstTimeJoining)
        {
            var master = player.master;
            var oldBody = master.GetBody();

            master.bodyPrefab = bodyPrefab;
            CharacterBody body;
            if (firstTimeJoining)
            {
                var spawnTransform = Stage.instance.GetPlayerSpawnTransform();
                body = master.SpawnBody(spawnTransform.position + _spawnOffset, spawnTransform.rotation);
                if (bodyPrefab.name == "HereticBody")
                {
                    master.inventory.GiveItem(RoR2Content.Items.LunarPrimaryReplacement, 1);
                    master.inventory.GiveItem(RoR2Content.Items.LunarSecondaryReplacement, 1);
                    master.inventory.GiveItem(RoR2Content.Items.LunarSpecialReplacement, 1);
                    master.inventory.GiveItem(RoR2Content.Items.LunarUtilityReplacement, 1);
                }
                Run.instance.HandlePlayerFirstEntryAnimation(body, spawnTransform.position + _spawnOffset, spawnTransform.rotation);
            }
            else
            {
                if (BodyCatalog.GetBodyName(oldBody.bodyIndex) == "CaptainBody")
                {
                    master.inventory.RemoveItem(RoR2Content.Items.CaptainDefenseMatrix, 1);
                }

                if (bodyPrefab.name == "CaptainBody")
                {
                    master.inventory.GiveItem(RoR2Content.Items.CaptainDefenseMatrix, 1);
                }

                if (BodyCatalog.GetBodyName(oldBody.bodyIndex) == "HereticBody")
                {
                    master.inventory.RemoveItem(RoR2Content.Items.LunarPrimaryReplacement, 1);
                    master.inventory.RemoveItem(RoR2Content.Items.LunarSecondaryReplacement, 1);
                    master.inventory.RemoveItem(RoR2Content.Items.LunarSpecialReplacement, 1);
                    master.inventory.RemoveItem(RoR2Content.Items.LunarUtilityReplacement, 1);
                }
                
                if (bodyPrefab.name != "HereticBody")
                {
                    body = master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
                }
                else
                {
                    if (bodyPrefab.name == "HereticBody")
                    {
                        master.inventory.GiveItem(RoR2Content.Items.LunarPrimaryReplacement, 1);
                        master.inventory.GiveItem(RoR2Content.Items.LunarSecondaryReplacement, 1);
                        master.inventory.GiveItem(RoR2Content.Items.LunarSpecialReplacement, 1);
                        master.inventory.GiveItem(RoR2Content.Items.LunarUtilityReplacement, 1);
                    }

                    body = master.GetBody();
                }
            }

            AddChatMessage($"{player.userName} is spawning as {body.GetDisplayName()}!");
        }

        private void JoinAs(NetworkUser user, string characterName, string username)
        {

            if (!DropInConfig.JoinAsEnabled)
            {
                Logger.LogWarning("Join_As disabled. Returning...");
                return;
            }

            if (DropInConfig.HostOnlySpawnAs)
            {
                if (NetworkUser.readOnlyInstancesList[0].netId != user.netId)
                {
                    Logger.LogWarning("HostOnlySpawnAs is enabled and the person using join_as isn't host. Returning!");
                    return;
                }
            }

            if (JoinAsBlocked)
            {
                SendJoinAsBlockedMessage();
                return;
            }

            //Finding the NetworkUser from the person who is using the command.
            NetworkUser player;
            // No user name provided, default to self
            if (username.IsNullOrWhiteSpace())
            {
                player = user;
            }
            else
            {
                player = GetNetUserFromString(username);
                if (player == null)
                {
                    AddChatMessage($"Could not find player with identifier: {username}");
                    return;
                }
            }

            //Finding the body the player wants to spawn as.
            GameObject bodyPrefab = BodyHelper.FindBodyPrefab(characterName);

            // The character the player is trying to spawn as doesn't exist. 
            if (!bodyPrefab)
            {
                AddChatMessage($"{characterName} not found. Availible survivors are: {string.Join(", ", BodyHelper.GetSurvivorDisplayNames())} (or use Random)");
                Logger.LogWarning("Sent message to player informing them that what they requested to join as does not exist. Also bodyPrefab does not exist, returning!");
                return;
            }
            
            if (player.master == null) // If the player is joining for the first time
            {
                Logger.LogInfo($"Spawning {player.userName} as newly joined player");

                // Make sure the person can actually join. This allows SetupUserCharacterMaster (which is called in OnUserAdded) to work.
                Run.instance.SetFieldValue("allowNewParticipants", true);

                //Now that we've made sure the person can join, let's give them a CharacterMaster.
                Run.instance.OnUserAdded(user);

                ChangeOrSetCharacter(player, bodyPrefab, true);

                // Turn this back off again so a new player isn't immediatly dropped in without getting to pick their character
                Run.instance.SetFieldValue("allowNewParticipants", false);
            }
            else // The player has already joined
            {
                Logger.LogInfo($"{player.userName} has already joined, checking other join conditions");

                if (!DropInConfig.AllowReJoinAs)
                {
                    Logger.LogInfo($"{player.userName} could not use join_as after selecting ");
                    AddChatMessage($"Sorry {player.userName}! The host has made it so you can't use join_as after selecting character.");
                }
                else if (player.master.lostBodyToDeath)
                {
                    Logger.LogInfo($"{player.userName} is dead and can't change character");
                    AddChatMessage($"Sorry {player.userName}! You can't use join_as while dead.");
                }
                else
                {
                    Logger.LogInfo($"Changing existing character for {player.userName}");
                    ChangeOrSetCharacter(player, bodyPrefab, false);
                }
            }
        }

        private void SendJoinAsBlockedMessage()
        {
            var reasons = string.Join(", ", _blockingReasons.Select(r => r.Value));
            AddChatMessage($"Sorry, join as is temporarily disabled for the following reason(s): {reasons}");
        }

        private NetworkUser GetNetUserFromString(string playerString)
        {
            if (playerString != "")
            {
                if (int.TryParse(playerString, out var result))
                {
                    if (result < NetworkUser.readOnlyInstancesList.Count && result >= 0)
                    {
                        return NetworkUser.readOnlyInstancesList[result];
                    }
                    Logger.LogError("Specified player index does not exist");
                    return null;
                }
                else
                {
                    foreach (NetworkUser n in NetworkUser.readOnlyInstancesList)
                    {
                        if (n.userName.Equals(playerString, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return n;
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        private void AddChatMessage(string message, float time = 0.1f)
        {
            StartCoroutine(AddHelperMessage(message, time));
        }

        private IEnumerator AddHelperMessage(string message, float time)
        {
            yield return new WaitForSeconds(time);
            var chatMessage = new Chat.SimpleChatMessage { baseToken = message };
            Chat.SendBroadcastChat(chatMessage);
        }
    }
}