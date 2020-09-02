using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Evaisa.FallenFriends;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace DropInMultiplayer
{
    [BepInPlugin(guid, modName, version)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.evaisa.fallenfriends", BepInDependency.DependencyFlags.SoftDependency)]
    [R2APISubmoduleDependency(nameof(CommandHelper))]
    public class DropInMultiplayer : BaseUnityPlugin
    {
        const string guid = "com.niwith.DropInMultiplayer";
        const string modName = "Drop In Multiplayer";
        const string version = "1.0.2";

        private DropInMultiplayerConfig _config;

        private readonly Vector3 _spawnOffset = new Vector3(0, 1, 0);

        private PluginInfo _fallenFriends = null;

        public void Awake()
        {
            _config = new DropInMultiplayerConfig(Config);
            SetupHooks();
            Logger.LogMessage("Drop-In Multiplayer Loaded!");
        }

        public void Start()
        {
            try
            {
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.evaisa.fallenfriends", out var fallenFriendsPlugin))
                {
                    // _fallenFriends = fallenFriendsPlugin.Instance as FallenFriends;
                    _fallenFriends = fallenFriendsPlugin;
                }
            }
            catch (Exception e)
            {
                Logger.LogFatal(e.StackTrace);
            }
        }

        private void SetupHooks()
        {
            On.RoR2.Console.RunCmd += CheckChatForJoinRequest;
            On.RoR2.NetworkUser.Start += GreetNewPlayer;
            On.RoR2.Run.SetupUserCharacterMaster += GiveItems;
            
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

        private void CheckChatForJoinRequest(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            orig(self, sender, concommandName, userArgs);

            if (concommandName.Equals("say", StringComparison.InvariantCultureIgnoreCase))
            {
                var userInput = userArgs.FirstOrDefault().Split(' ');
                var chatCommand = userInput.FirstOrDefault();
                if (chatCommand.IsNullOrWhiteSpace())
                {
                    return;
                }

                if (chatCommand.Equals("join_as", StringComparison.InvariantCultureIgnoreCase) || chatCommand.Equals("join", StringComparison.InvariantCultureIgnoreCase))
                {
                    string bodyString = userInput.ElementAtOrDefault(1) ?? "";
                    string userString = userInput.ElementAtOrDefault(2) ?? "";

                    JoinAs(sender.networkUser, bodyString, userString);
                }
            }
        }

        private void GreetNewPlayer(On.RoR2.NetworkUser.orig_Start orig, NetworkUser self)
        {
            orig(self);
            if (NetworkServer.active && Stage.instance != null && //Make sure we're host.
                _config.WelcomeMessage) //If the host man has enabled this config option.
            {
                AddChatMessage("Hello " + self.userName + $"! Join the game by typing 'join [character name]' or 'join_as [character name]' in chat (without the apostrophes of course) into the chat. Available survivors are: { string.Join(", ", BodyHelper.GetSurvivorDisplayNames())}", 1f);
            }
        }

        [Server]
        private void GiveItems(On.RoR2.Run.orig_SetupUserCharacterMaster orig, Run run, NetworkUser user)
        {
            orig(run, user);
            
            if (!_config.StartWithItems ||
                !run.isServer || // If we are not the server don't try to give items, let the server handle it
                run.fixedTime < 5f) // Don't try to give items to players who spawn with the server
            {
                Debug.Log("We are not giving items for some reason, figure that out, might be we aint server");
                return;
            }

            if (_config.GiveExactItems)
            {
                Debug.Log("Giving exact items");
                ItemsHelper.CopyItemsFromRandom(user);
            }
            else
            {
                Debug.Log("Giving averaged items");
                ItemsHelper.GiveAveragedItems(user, _config.GiveRedItems, _config.GiveLunarItems, _config.GiveBossItems);
            }
        }

        private void ChangeOrSetCharacter(NetworkUser player, GameObject bodyPrefab, bool firstTimeJoining)
        {
            var master = player.master;
            master.bodyPrefab = bodyPrefab;
            _fallenFriends?.Instance.InvokeMethod("setOldPrefab", master);
            //_fallenFriends?.setOldPrefab(master);
            CharacterBody body;
            if (firstTimeJoining)
            {
                var spawnTransform = Stage.instance.GetPlayerSpawnTransform();
                body = master.SpawnBody(bodyPrefab, spawnTransform.position + _spawnOffset, spawnTransform.rotation);               
                Run.instance.HandlePlayerFirstEntryAnimation(body, spawnTransform.position + _spawnOffset, spawnTransform.rotation);
            }
            else
            {
                body = master.Respawn(master.GetBody().transform.position, master.GetBody().transform.rotation);
            }
            
            AddChatMessage($"{player.userName} is spawning as {body.GetDisplayName()}!");
        }

        private void JoinAs(NetworkUser user, string characterName, string username)
        {
            if (!_config.JoinAsEnabled)
            {
                Logger.LogWarning("JoinAs :: SpawnAsEnabled.Value disabled. Returning...");
                return;
            }

            if (_config.HostOnlySpawnAs)
            {
                if (NetworkUser.readOnlyInstancesList[0].netId != user.netId)
                {
                    Logger.LogWarning("JoinAs :: HostOnlySpawnAs.Value enabled and the person using join_as isn't host. Returning!");
                    return;
                }
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
                AddChatMessage($"Sorry {player.userName} couldn't find {characterName}. Availible survivors are: {string.Join(", ", BodyHelper.GetSurvivorDisplayNames())}");
                Logger.LogWarning("JoinAs :: Sent message to player informing them that what they requested to join as does not exist. Also bodyPrefab does not exist, returning!");
                return;
            }

            if (player.master == null) // If the player is joining for the first time
            {
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
                if (!_config.AllowReJoinAs)
                {
                    AddChatMessage($"Sorry {player.userName}! The host has made it so you can't use join_as while after selecting character.");
                }
                else
                {
                    var master = player.master;
                    if (master.hasBody)
                    {
                        ChangeOrSetCharacter(player, bodyPrefab, false);
                    }
                }
            }
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