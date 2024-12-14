using BepInEx;
using BepInEx.Logging;
using DropInMultiplayer.Helpers;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace DropInMultiplayer
{
    internal enum JoinAsResult
    {
        Success,
        DeadAndNotAllowRespawn
    }

    internal class ChatCommand
    {
        public string Name { get; set; }
        public string HelpText { get; set; }
        public Func<NetworkUser, string[], string> Handler { get; set; }

        internal ChatCommand(string name, string helpText, Func<NetworkUser, string[], string> handler)
        {
            Name = name;
            HelpText = helpText;
            Handler = handler;
        }
    }

    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    public class DropInMultiplayer : BaseUnityPlugin
    {
        public const string PluginGUID = "com.niwith.DropInMultiplayer";
        public const string PluginName = "Drop In Multiplayer";
        public const string PluginVersion = "3.0.0";

        private const string HelpHelpText = "Usage: help {command}\nDescription: Prints help text for command";
        private const string JoinAsHelpText = "Usage: join_as {survivor} {player (optional)}\nDescription: Join in-progress run as the given survivor";
        private const string ListSurvivorsHelpText = "Usage: list_survivors {player (optional)}\nDescription: Shows a list of all availible survivors for given player (or self)";
        private const string ListBodiesHelpText = "Usage: list_bodies\nDescription: Shows a list of all bodies to be used with join_as command (if AllowJoinAsAllBodies is true)";
        private const string CatchupHelpText = "Usage: catchup {player (optional)}\nDescription: Manually adds catch up items for the given player (or self)";
        private const string GiveRandomItemsHelpText = "Usage: give_random_items {count} {lunarEnabled} {voidEnabled} {player (optional)}\nDescription: Adds random items for the given player (or self)";

        public static DropInMultiplayerConfig DropInConfig { get; set; }
        public static DropInMultiplayer Instance { get; set; }
        internal static new ManualLogSource Logger { get; set; }

        private static readonly System.Random _rand = new System.Random();

        private static readonly Dictionary<string, ChatCommand> _chatCommands = new List<ChatCommand>()
        {
            new ChatCommand("HELP", HelpHelpText, Help),
            new ChatCommand("JOIN", JoinAsHelpText, JoinAs),
            new ChatCommand("JOIN_AS", JoinAsHelpText, JoinAs),
            new ChatCommand("LIST_SURVIVORS", ListSurvivorsHelpText, ListSurvivors),
            new ChatCommand("LIST_BODIES", ListBodiesHelpText, ListBodies),
#if DEBUG
            new ChatCommand("CATCHUP", CatchupHelpText, Catchup),
            new ChatCommand("GIVE_RANDOM_ITEMS", GiveRandomItemsHelpText, GiveRandomItems)
#endif
        }.ToDictionary(rec => rec.Name);

        //Move/rename this to wherever you see fit.
        private static HashSet<Inventory> captainBlacklistInventories;

        public void Awake()
        {
            Instance = this;
            Logger = base.Logger;
            DropInConfig = new DropInMultiplayerConfig(Config);

            SetupEventHandlers();
        }

        private static void SetupEventHandlers()
        {
            RoR2.Run.onRunStartGlobal += Run_onRunStartGlobal;
            On.RoR2.Console.RunCmd += Console_RunCmd;
            On.RoR2.Run.SetupUserCharacterMaster += Run_SetupUserCharacterMaster;
            NetworkUser.onPostNetworkUserStart += NetworkUser_onPostNetworkUserStart;

#if DEBUG
            Logger.LogWarning("You're on a debug build. If you see this after downloading from the thunderstore, panic!");
            //This is so we can connect to ourselves.
            //Instructions:
            //Step One: Assuming this line is in your codebase, start two instances of RoR2 (do this through the .exe directly)
            //Step Two: Host a game with one instance of RoR2.
            //Step Three: On the instance that isn't hosting, open up the console (ctrl + alt + tilde) and enter the command "connect localhost:7777"
            //DO NOT MAKE A MISTAKE SPELLING THE COMMAND OR YOU WILL HAVE TO RESTART THE CLIENT INSTANCE!!
            //Step Four: Test whatever you were going to test.
            On.RoR2.Networking.NetworkManagerSystem.ClientSendAuth += (orig, self, conn) => { };
#endif
        }

        private static void Run_onRunStartGlobal(Run run)
        {
            //Reset this on new run
            captainBlacklistInventories = new HashSet<Inventory>();
        }

        private static void Run_SetupUserCharacterMaster(On.RoR2.Run.orig_SetupUserCharacterMaster orig, Run self, NetworkUser user)
        {
            // This method always throws null reference exception for new player joining after game starts, but not when called
            // called again to spawn the player in after they use the join_as command
            // This method throwing an uncaught exception causes the following lines in NetworkUser.Start not to be called:
            //
            //if (NetworkClient.active)
            //{
            //    SyncLunarCoinsToServer();
            //    SendServerUnlockables();
            //}
            //
            //OnLoadoutUpdated();
            //NetworkUser.onPostNetworkUserStart?.Invoke(this);
            //if (base.isLocalPlayer && (bool)Stage.instance)
            //{
            //    CallCmdAcknowledgeStage(Stage.instance.netId);
            //}
            //
            // Which I believe causes issues like skins not being displayed to other users correct (OnLoadoutUpdated())
            // So we are just going to go ahead and catch the exception. As far as I am aware this error is not caused by DropInMultiplayer being installed
            // If anyone reads this and knows what causes this issue or a better fix for the issue feel free to send me a message

            try
            {
                orig(self, user);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Logger.LogError(ex);
                Logger.LogMessage("SetupUserCharacterMaster threw an exception and was caught");
            }
        }

        private static void Console_RunCmd(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            orig(self, sender, concommandName, userArgs);

            if (!NetworkServer.active || Run.instance == null)
            {
                return;
            }

            if (!concommandName.Equals("say", StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            string chatMessage = userArgs.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(chatMessage) || !chatMessage.StartsWith("/"))
            {
                return;
            }
            string[] splitMessage = chatMessage.Split(new char[] { ' ' });
            string chatCommandName = splitMessage.FirstOrDefault().Substring(1); // substring removes leading slash
            string[] commandArgs = splitMessage.Skip(1).ToArray();


            if (!_chatCommands.TryGetValue(chatCommandName.ToUpperInvariant(), out ChatCommand chatCommand))
            {
                SendChatMessage("Unable to find command, try /help");
            }

            string resultMessage = chatCommand.Handler(sender.networkUser, commandArgs);
            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                SendChatMessage(resultMessage);
            }
        }

        private static void NetworkUser_onPostNetworkUserStart(NetworkUser networkUser)
        {
            if (NetworkServer.active && Run.instance != null)
            {
                bool isPreviousPlayer = networkUser.master != null;

                Logger.LogMessage($"{networkUser.userName} has joined {(isPreviousPlayer ? "as a previously connected player" : "as a new player")}");
                GreetNewPlayer(networkUser);

                if (DropInConfig.JoinAsRandomByDefault.Value && !isPreviousPlayer)
                {
                    Logger.LogMessage($"Spawning new player as random, since JoinAsRandomByDefault is true");
                    // Let init for network user finish then spawn them in
                    Instance.StartCoroutine(SpawnPlayerAsRandomInternal(networkUser));
                }

                if (DropInConfig.GiveCatchUpItems.Value && DropInConfig.GiveRejoiningPlayersCatchUpItems.Value && isPreviousPlayer)
                {
                    GiveCatchUpItems(networkUser);
                }
            }
        }

        /// <summary>
        /// Internal method which performs the logic to spawn the given 
        /// player as the given character
        /// as the given character
        /// </summary>
        /// <param name="player">Network user to spawn</param>
        /// <param name="newBodyPrefab">New body prefab to spawn the player as</param>
        private static JoinAsResult SpawnPlayerWithBody(NetworkUser player, BodyIndex newBodyIndex)
        {
            if (player.master == null) // master is null for newly joining players
            {
                Logger.LogMessage($"Spawning new player {player.userName} with bodyIndex = {newBodyIndex}");
                return SpawnNewPlayerWithBody(player, newBodyIndex);
            }
            else // reconnecting players or players trying to change character
            {
                Logger.LogMessage($"Respawning existing player {player.userName} with bodyIndex = {newBodyIndex}");
                return RespawnExistingPlayerWithBody(player, newBodyIndex);
            }
        }

        private static JoinAsResult SpawnNewPlayerWithBody(NetworkUser player, BodyIndex newBodyIndex)
        {
            player.CmdSetBodyPreference(newBodyIndex);

            Run.instance.SetFieldValue("allowNewParticipants", true);
            Run.instance.OnUserAdded(player);
            Run.instance.SetFieldValue("allowNewParticipants", false);


            Transform spawnTransform = GetSpawnTransformForPlayer(player);
            player.master.SpawnBody(spawnTransform.position, spawnTransform.rotation);

            HandleBodyItems(player, null, BodyCatalog.GetBodyPrefab(newBodyIndex));

            if (DropInConfig.GiveCatchUpItems.Value)
            {
                GiveCatchUpItems(player);
            }

            return JoinAsResult.Success;
        }

        private static JoinAsResult RespawnExistingPlayerWithBody(NetworkUser player, BodyIndex newBodyIndex)
        {
            GameObject oldBodyPrefab = player.master.bodyPrefab;
            player.CmdSetBodyPreference(newBodyIndex);

            JoinAsResult result = JoinAsResult.Success;

            if (player.GetCurrentBody() == null && player.master.lostBodyToDeath && !DropInConfig.AllowRespawn.Value)
            {
                Logger.LogMessage($"Unable immediately to spawn {player.userName} with bodyIndex = {newBodyIndex} due to being player being dead and AllowRespawn being set to false");
                result = JoinAsResult.DeadAndNotAllowRespawn;
            }
            else
            {
                Transform spawnTransform = GetSpawnTransformForPlayer(player);
                player.master.Respawn(spawnTransform.position, spawnTransform.rotation);
            }

            HandleBodyItems(player, oldBodyPrefab, BodyCatalog.GetBodyPrefab(newBodyIndex));

            return result;
        }

        private static void HandleBodyItems(NetworkUser player, GameObject oldBodyPrefab, GameObject newBodyPrefab)
        {
            string oldBodyName = null;
            string newBodyName = null;

            try
            {
                Inventory playerInventory = player.master.inventory;
                oldBodyName = oldBodyPrefab?.name; // Null when first joining and don't have a body prefab to switch from
                newBodyName = newBodyPrefab.name;

                switch(oldBodyName)
                {
                    case "CaptainBody":
                        bool hasMicrobots = playerInventory.GetItemCount(RoR2Content.Items.CaptainDefenseMatrix) > 0;
                        if (!hasMicrobots && DropInConfig.PreventCaptainScrapAbuse.Value)
                        {
                            captainBlacklistInventories.Add(playerInventory);
                        }

                        //This case is for mod setups where Microbots can be obtained via drops.
                        //If a player is already blacklisted from auto-receiving Microbots, there's no need to remove it.
                        if (!captainBlacklistInventories.Contains(playerInventory))
                        {
                            playerInventory.RemoveItem(RoR2Content.Items.CaptainDefenseMatrix);
                        }
                        break;
                    case "HereticBody":
                        if (DropInConfig.GiveHereticItems.Value)
                        {
                            playerInventory.RemoveItem(RoR2Content.Items.LunarPrimaryReplacement);
                            playerInventory.RemoveItem(RoR2Content.Items.LunarSecondaryReplacement);
                            playerInventory.RemoveItem(RoR2Content.Items.LunarSpecialReplacement);
                            playerInventory.RemoveItem(RoR2Content.Items.LunarUtilityReplacement);
                        }
                        break;
                    default:
                        break;
                }

                switch(newBodyName)
                {
                    case "CaptainBody":
                        if (!captainBlacklistInventories.Contains(playerInventory) || !DropInConfig.PreventCaptainScrapAbuse.Value)
                        {
                            playerInventory.GiveItem(RoR2Content.Items.CaptainDefenseMatrix);
                        }
                        break;
                    case "HereticBody":
                        if (DropInConfig.GiveHereticItems.Value)
                        {
                            playerInventory.GiveItem(RoR2Content.Items.LunarPrimaryReplacement);
                            playerInventory.GiveItem(RoR2Content.Items.LunarSecondaryReplacement);
                            playerInventory.GiveItem(RoR2Content.Items.LunarSpecialReplacement);
                            playerInventory.GiveItem(RoR2Content.Items.LunarUtilityReplacement);
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Logger.LogError(ex);
                Logger.LogMessage($"Handling body items for transition from {oldBodyName ?? "none"} to {newBodyName ?? "none"} resulted in an exception");
            }
        }

        #region Command Logic
        private static string Help(NetworkUser sender, string[] args)
        {
            if (args.Length > 1)
            {
                return "Help requires either 0 or 1 argument";
            }

            if (args.Length == 1)
            {
                if (!_chatCommands.TryGetValue(args[0].ToUpperInvariant(), out ChatCommand chatCommand))
                {
                    return "Unable to find command, try /help";
                }

                return chatCommand.HelpText;
            }
            else
            {
                return $"Availible Commands: {string.Join(",", _chatCommands.Values.Select(command => command.Name.ToLower()))}";
            }
        }

        private static string JoinAs(NetworkUser sender, string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                return "join as requires either 1 or 2 arguments";
            }

            NetworkUser player = args.Length == 1 ? sender : GetNetUserFromString(args[1]);
            if (player == null)
            {
                return "Unable to find player with given name";
            }

            string survivorOrBodyName = args[0];
            string displayName;

            SurvivorDef survivor = GetAvailibleSurvivorForPlayerByName(player, survivorOrBodyName);
            BodyIndex spawnAsBodyIndex = BodyIndex.None;
            if (survivor != null)
            {
                spawnAsBodyIndex = BodyCatalog.FindBodyIndex(survivor.bodyPrefab);
                displayName = Language.GetString(survivor.displayNameToken);
            }
            else if (DropInConfig.AllowJoinAsAllBodies.Value)
            {
                BodyIndex bodyIndex = BodyCatalog.FindBodyIndexCaseInsensitive(survivorOrBodyName.EndsWith("Body", StringComparison.InvariantCultureIgnoreCase) ? survivorOrBodyName : survivorOrBodyName + "Body");
                if (bodyIndex == BodyIndex.None)
                {
                    return "Unable to find survivor or body with given name";
                }

                spawnAsBodyIndex = bodyIndex;
                displayName = BodyCatalog.GetBodyName(bodyIndex);
            }
            else
            {
                return "Unable to find survivor with the given name";
            }

            JoinAsResult joinAsResult;
            try
            {
                joinAsResult = SpawnPlayerWithBody(player, spawnAsBodyIndex);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Logger.LogError(ex);
                return $"An exception occured spawning {player.userName} as {displayName}";
            }

            switch (joinAsResult)
            {
                case JoinAsResult.Success:
                    return $"Spawning {player.userName} as {displayName}";
                case JoinAsResult.DeadAndNotAllowRespawn:
                    return $"{player.userName} will be spawned as {displayName} next stage";
                default:
                    return "Unknown join as result";
            }
        }

        private static string ListSurvivors(NetworkUser sender, string[] args)
        {
            if (args.Length > 1)
            {
                return "list survivors requires 0 or 1 argument";
            }

            NetworkUser player = args.Length == 0 ? sender : GetNetUserFromString(args[0]);
            if (player == null)
            {
                return "Unable to find player with given name";
            }

            return GetSurvivorChatListForPlayer(player);
        }

        private static string ListBodies(NetworkUser sender, string[] args)
        {
            if (args.Length > 0)
            {
                return "list bodies requires no arguments";
            }

            return string.Join(", ", BodyCatalog.allBodyPrefabs.Select(prefab => prefab.name));
        }

        private static string Transform(NetworkUser sender, string[] args)
        {
            if (args.Length > 2 || args.Length == 0)
            {
                return "transform requires 1 or 2 arguments";
            }

            NetworkUser player = args.Length == 1 ? sender : GetNetUserFromString(args[1]);
            if (player == null)
            {
                return "Unable to find player with given name";
            }

            player.master.TransformBody(args[0]);

            return $"Transformed {sender.userName} into {args[0]}";
        }

        private static string Catchup(NetworkUser sender, string[] args)
        {
            if (args.Length != 0 && args.Length != 1)
            {
                return "Catchup requires 0 or 1 argument";
            }

            NetworkUser player = args.Length == 0 ? sender : GetNetUserFromString(args[0]);
            if (player == null)
            {
                return "Unable to find player with given name";
            }

            GiveCatchUpItems(player);

            return $"Gave {player.userName} catch up items";
        }

        private static string GiveRandomItems(NetworkUser sender, string[] args)
        {
            if (args.Length != 3 && args.Length != 4)
            {
                return "Give random items requires 3 or 4 arguments";
            }

            NetworkUser player = args.Length == 3 ? sender : GetNetUserFromString(args[3]);
            if (player == null)
            {
                return "Unable to find player with given name";
            }

            if (player?.master?.inventory == null)
            {
                return "Player has no inventory so cannot be given items";
            }

            int count;
            bool lunarEnabled;
            bool voidEnabled;

            try
            {
                count = int.Parse(args[0]);
                lunarEnabled = bool.Parse(args[1]);
                voidEnabled = bool.Parse(args[2]);
            }
            catch
            {
                return "Unable to parse arguments";
            }

            GiveRandomItems(player.master.inventory, count, lunarEnabled, voidEnabled);

            return $"Gave {player.userName} {count} random items";
        }

        #endregion

        #region Console Commands
        [ConCommand(commandName = "dim_join_as", flags = ConVarFlags.ExecuteOnServer, helpText = JoinAsHelpText)]
        private static void CommandJoinAs(ConCommandArgs args)
        {
            Debug.Log(JoinAs(args.sender, args.userArgs.ToArray()));
        }

        [ConCommand(commandName = "dim_list_survivors", flags = ConVarFlags.ExecuteOnServer, helpText = ListSurvivorsHelpText)]
        private static void CommandShowSurvivors(ConCommandArgs args)
        {
            Debug.Log(ListSurvivors(args.sender, args.userArgs.ToArray()));
        }

        [ConCommand(commandName = "dim_list_bodies", flags = ConVarFlags.ExecuteOnServer, helpText = ListBodiesHelpText)]
        private static void CommandListBodies(ConCommandArgs args)
        {
            Debug.Log(ListBodies(args.sender, args.userArgs.ToArray()));
        }

#if DEBUG
        [ConCommand(commandName = "dim_catchup", flags = ConVarFlags.ExecuteOnServer, helpText = CatchupHelpText)]
        private static void CommandCatchup(ConCommandArgs args)
        {
            Debug.Log(Catchup(args.sender, args.userArgs.ToArray()));
        }

        [ConCommand(commandName = "dim_give_random_items", flags = ConVarFlags.ExecuteOnServer, helpText = GiveRandomItemsHelpText)]
        private static void CommandGiveRandomItems(ConCommandArgs args)
        {
            Debug.Log(GiveRandomItems(args.sender, args.userArgs.ToArray()));
        }
#endif
        #endregion

        #region Helpers
        private static NetworkUser GetNetUserFromString(string playerString)
        {
            if (!string.IsNullOrWhiteSpace(playerString))
            {
                if (int.TryParse(playerString, out int playerIndex))
                {
                    if (playerIndex < NetworkUser.readOnlyInstancesList.Count && playerIndex >= 0)
                    {
                        return NetworkUser.readOnlyInstancesList[playerIndex];
                    }
                    return null;
                }
                else
                {
                    foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
                    {
                        if (networkUser.userName.Replace(" ", "").Equals(playerString.Replace(" ", ""), StringComparison.InvariantCultureIgnoreCase))
                        {
                            return networkUser;
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets array of availible surviors to join as for a player, based on run unlocks.
        /// 
        /// Code inspired by the <see cref="RoR2.CharacterMaster.PickRandomSurvivorBodyPrefab"/> method
        /// </summary>
        /// <param name="player">Player to get availble survivors for</param>
        /// <returns>Array of availble survivors</returns>
        public static SurvivorDef[] GetAvailibleSurvivorsForPlayer(NetworkUser player)
        {
            return SurvivorCatalog.allSurvivorDefs.Where(SurvivorIsUnlockedAndAvailable).ToArray();

            bool SurvivorIsUnlockedAndAvailable(SurvivorDef survivorDef)
            {
                if (!DropInConfig.AllowJoinAsHiddenSurvivors.Value && survivorDef.hidden)
                {
                    Logger.LogMessage($"Survivor {survivorDef.cachedName} is not availible because survivor is hidden and AllowJoinAsHiddenSurvivors is false");
                    return false;
                }

                // Should figure out at some point how to make this work, the following code will prevent player from joining as anything that
                // wasn't unlocked by the players who started the run
                //if (survivorDef.unlockableDef != null && !Run.instance.IsUnlockableUnlocked(survivorDef.unlockableDef))
                //{
                //    Logger.LogMessage($"Survivor {survivorDef.cachedName} is not availible because survivor is not unlocked");
                //    return false;
                //}
                //if (survivorDef.CheckUserHasRequiredEntitlement(player))
                //{
                //    Logger.LogMessage($"Survivor {survivorDef.cachedName} is not availible because player does not have required entitlement");
                //    return false;
                //}
                //if (!survivorDef.CheckRequiredExpansionEnabled())
                //{
                //    Logger.LogMessage($"Survivor {survivorDef.cachedName} is not availible because required expansion is not enabled");
                //    return false;
                //}

                return true;
            }
        }

        /// <summary>
        /// Gets the survivor by given name from the player's availible survivors. 
        /// When "random" is given as survivor name a random survivor will be selected
        /// Returns null when survivor can not be found, or player does not have that survivor availible
        /// </summary>
        /// <param name="player">Player to get survivor for</param>
        /// <param name="name">Name of the survivor to retrieve</param>
        /// <returns></returns>
        public static SurvivorDef GetAvailibleSurvivorForPlayerByName(NetworkUser player, string name)
        {
            SurvivorDef[] availibleSurvivors = GetAvailibleSurvivorsForPlayer(player);
            if (string.Equals(name, "random", StringComparison.InvariantCultureIgnoreCase))
            {
                return availibleSurvivors[_rand.Next(availibleSurvivors.Length)];
            }
            else
            {
                return availibleSurvivors.Where(NameStringMatches).FirstOrDefault();
            }

            bool NameStringMatches(SurvivorDef survivorDef)
            {
                return string.Equals(survivorDef.cachedName, name, StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(Language.GetString(survivorDef.displayNameToken).Replace(" ", ""), name.Replace(" ", ""), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private static void GiveCatchUpItems(NetworkUser player)
        {
            List<PickupIndex> validCountItems = new List<PickupIndex>();
            validCountItems.AddRange(Run.instance.availableTier1DropList);
            validCountItems.AddRange(Run.instance.availableTier2DropList);
            validCountItems.AddRange(Run.instance.availableTier3DropList);

            validCountItems.AddRange(Run.instance.availableBossDropList);

            validCountItems.AddRange(Run.instance.availableVoidTier1DropList);
            validCountItems.AddRange(Run.instance.availableVoidTier2DropList);
            validCountItems.AddRange(Run.instance.availableVoidTier3DropList);

            validCountItems.AddRange(Run.instance.availableVoidBossDropList);

            validCountItems.AddRange(Run.instance.availableLunarItemDropList);

            Inventory[] activePlayerInventories = NetworkUser.readOnlyInstancesList.Where(netUser => netUser != player && netUser?.master?.inventory != null).Select(netUser => netUser.master.inventory).ToArray();
            ItemIndex[] countItemIndexes = validCountItems.Select(pickupIndex => PickupCatalog.GetPickupDef(pickupIndex).itemIndex).ToArray();

            int averageItemCount = (int)activePlayerInventories.Average(CountInventoryItems);
            int itemsToGive = averageItemCount - CountInventoryItems(player.master.inventory);

            Logger.LogMessage($"On average other players have {averageItemCount} items, giving {player.userName} {itemsToGive} items to match");
            GiveRandomItems(player.master.inventory, itemsToGive, DropInConfig.GiveCatchUpLunarItems.Value, DropInConfig.GiveCatchUpVoidItems.Value);

            int CountInventoryItems(Inventory inventory)
            {
                return countItemIndexes.Sum(itemIndex => inventory.GetItemCount(itemIndex));
            }
        }

        /// <summary>
        /// Lifted straight from <seealso cref="RoR2.Inventory.GiveRandomItems"/>, which seems to currenlty have a bug with
        /// only giving tier 1 void items. At some point i'll remove this and point directly at the internal give random items method
        /// </summary>
        /// <param name="count">Number of items to give</param>
        /// <param name="lunarEnabled">When enabled will add lunar items to the list to picked from</param>
        /// <param name="voidEnabled">When enabled will add void items to the list picked from</param>
        private static void GiveRandomItems(Inventory inventory, int count, bool lunarEnabled, bool voidEnabled)
        {
            if (count > 0)
            {
                WeightedSelection<List<PickupIndex>> weightedSelection = new WeightedSelection<List<PickupIndex>>();
                weightedSelection.AddChoice(Run.instance.availableTier1DropList, 100f);
                weightedSelection.AddChoice(Run.instance.availableTier2DropList, 60f);
                weightedSelection.AddChoice(Run.instance.availableTier3DropList, 4f);
                if (lunarEnabled)
                {
                    weightedSelection.AddChoice(Run.instance.availableLunarItemDropList, 4f);
                }
                if (voidEnabled)
                {
                    weightedSelection.AddChoice(Run.instance.availableVoidTier1DropList, 4f);
                    weightedSelection.AddChoice(Run.instance.availableVoidTier2DropList, 2.39999986f);
                    weightedSelection.AddChoice(Run.instance.availableVoidTier3DropList, 0.16f);
                }
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        List<PickupIndex> tierDropList = weightedSelection.Evaluate(UnityEngine.Random.value);
                        PickupIndex itemPickupIndex = tierDropList.ElementAtOrDefault(UnityEngine.Random.Range(0, tierDropList.Count));
                        ItemIndex itemIndex = PickupCatalog.GetPickupDef(itemPickupIndex)?.itemIndex ?? ItemIndex.None;
                        inventory.GiveItem(itemIndex);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Logger.LogError(ex);
                        Logger.LogMessage("Exception occured giving player an item");
                    }
                }
            }
        }

        // Spawn on another player (or self) by default to avoid softlocking by spawning outside a restricted area
        private static Transform GetSpawnTransformForPlayer(NetworkUser player)
        {
            Transform spawnTransform = player.GetCurrentBody()?.transform;

            if (spawnTransform == null)
            {
                Logger.LogMessage($"{player.userName} does not have a current body, spawning on another player");
                spawnTransform = NetworkUser.readOnlyInstancesList
                    .Where(user => user.GetCurrentBody() != null).FirstOrDefault()?.GetCurrentBody().transform;
            }

            if (spawnTransform == null)
            {
                Logger.LogMessage($"Unable to find alive player for {player.userName} to spawn on, defaulting to map spawn");
                spawnTransform = Stage.instance.GetPlayerSpawnTransform();
            }

            return spawnTransform;
        }

        private static IEnumerator SpawnPlayerAsRandomInternal(NetworkUser player)
        {
            yield return new WaitForSeconds(0.1f);
            SpawnPlayerWithBody(player, BodyCatalog.FindBodyIndex(GetAvailibleSurvivorForPlayerByName(player, "random").bodyPrefab));
        }

        #endregion

        #region ChatMessages
        private static string GetSurvivorChatListForPlayer(NetworkUser player)
        {
            SurvivorDef[] survivors = GetAvailibleSurvivorsForPlayer(player);
            string[] survivorDisplayStrings = survivors.Select(survivor => $"{survivor.cachedName} ({Language.GetString(survivor.displayNameToken)})").ToArray();
            return string.Join(", ", survivorDisplayStrings);
        }

        private static void GreetNewPlayer(NetworkUser player)
        {
            if (!DropInConfig.SendWelcomeMessage.Value)
            {
                return;
            }

            string greetingMessageFormat = DropInConfig.CustomWelcomeMessage.Value;
            if (greetingMessageFormat.Length > 1000)
            {
                Logger.LogMessage($"The custom welcome message has a length of {greetingMessageFormat.Length} which is longer than the limit of 1000 characters");
                return;
            }

            string greetingMessage = greetingMessageFormat
                .ReplaceOnce("{username}", player.userName)
                .ReplaceOnce("{survivorlist}", GetSurvivorChatListForPlayer(player));

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage
            {
                baseToken = greetingMessage
            });
        }

        private static void SendChatMessage(string message)
        {
            Instance.StartCoroutine(SendChatMessageInternal(message));
        }

        private static IEnumerator SendChatMessageInternal(string message)
        {
            yield return new WaitForSeconds(0.1f);
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = message });
        }
        #endregion
    }
}
