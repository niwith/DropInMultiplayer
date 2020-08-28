using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace DropInMultiplayer
{
    [BepInPlugin(guid, modName, version)]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    public class DropInMultiplayer : BaseUnityPlugin
    {
        const string guid = "com.AwokeinanEngima.DropInMultiplayer";
        const string modName = "Drop In Multiplayer";
        const string version = "1.0.0";
        
        private DropInMultiplayerConfig _config;

        private ItemIndex GetRandomItem(List<ItemIndex> items)
        {
            int itemID = UnityEngine.Random.Range(0, items.Count);

            return items[itemID];
        }

        public void Awake()
        {
            _config = new DropInMultiplayerConfig(Config);
            SetupHooks();
            Logger.LogMessage("Drop-In Multiplayer Loaded!");
        }

        private void SetupHooks() {
            On.RoR2.Run.SetupUserCharacterMaster += GiveItems;
            On.RoR2.Console.RunCmd += CheckforJoinAsRequest;
            On.RoR2.NetworkUser.Start += GreetNewPlayer;

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

        private void CheckforJoinAsRequest(On.RoR2.Console.orig_RunCmd orig, RoR2.Console self, RoR2.Console.CmdSender sender, string concommandName, List<string> userArgs)
        {
            orig(self, sender, concommandName, userArgs);
            if (concommandName.Equals("say", StringComparison.InvariantCultureIgnoreCase)) {
                var userInput = userArgs.FirstOrDefault().Split(' ');
                if (!(userInput.FirstOrDefault() ?? "").Equals("join_as", StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }
                string bodyString = userInput.ElementAtOrDefault(1) ?? "";
                string userString = userInput.ElementAtOrDefault(2) ?? "";

                JoinAs(sender.networkUser, bodyString, userString);
            }
        }

        private void GreetNewPlayer(On.RoR2.NetworkUser.orig_Start orig, NetworkUser self)
        {
            orig(self);
            if (NetworkServer.active && Stage.instance != null && //Make sure we're host.
                _config.WelcomeMessage) //If the host man has enabled this config option.
            { 
                AddChatMessage("Hello " + self.userName + $"! Join the game by typing 'join_as [character name]' (without the apostrophes of course) into the chat. Availible survivors are: { string.Join(", ", BodyHelper.GetSurvivorNames())}", 1f);
            }
        }

        private void GiveItems(On.RoR2.Run.orig_SetupUserCharacterMaster orig, Run self, NetworkUser user)
        {
            orig(self, user);

            try
            {
                /*
             * **********************************************************************
             * more or less copied from https://github.com/xiaoxiao921/DropInMultiplayer/blob/master/DropInMultiplayerFix/Main.cs
             * **********************************************************************
             */
                CharacterMaster characterMaster = user.master;
                if (!_config.StartWithItems || 
                    Run.instance.fixedTime < 5f ||
                    characterMaster == null)
                {
                    return;
                }

                var otherPlayerCharacters = NetworkUser.readOnlyInstancesList.Where(player => !player.id.Equals(user.id)).Select(p => p.master).ToArray();
                if (otherPlayerCharacters.Length <= 0) // We are the only player
                {
                    return;
                }

                var averageItemCountT1 = (int)otherPlayerCharacters.Average(p => p.inventory.GetTotalItemCountOfTier(ItemTier.Tier1));
                var averageItemCountT2 = (int)otherPlayerCharacters.Average(p => p.inventory.GetTotalItemCountOfTier(ItemTier.Tier2));
                var averageItemCountT3 = (int)otherPlayerCharacters.Average(p => p.inventory.GetTotalItemCountOfTier(ItemTier.Tier3));
                var averageItemCountTL = (int)otherPlayerCharacters.Average(p => p.inventory.GetTotalItemCountOfTier(ItemTier.Lunar));
                var averageItemCountTB = (int)otherPlayerCharacters.Average(p => p.inventory.GetTotalItemCountOfTier(ItemTier.Boss));

                int itemCountT1 = averageItemCountT1 - characterMaster.inventory.GetTotalItemCountOfTier(ItemTier.Tier1);
                int itemCountT2 = averageItemCountT2 - characterMaster.inventory.GetTotalItemCountOfTier(ItemTier.Tier2);
                int itemCountT3 = averageItemCountT3 - characterMaster.inventory.GetTotalItemCountOfTier(ItemTier.Tier3);
                int itemCountTL = averageItemCountTL - characterMaster.inventory.GetTotalItemCountOfTier(ItemTier.Lunar);

                for (int i = 0; i < itemCountT1; i++)
                {
                    characterMaster.inventory.GiveItem(GetRandomItem(ItemCatalog.tier1ItemList), 1);
                }

                for (int i = 0; i < itemCountT2; i++)
                {
                    characterMaster.inventory.GiveItem(GetRandomItem(ItemCatalog.tier2ItemList), 1);
                }

                if (_config.GiveRedItems)
                {
                    for (int i = 0; i < itemCountT3; i++)
                    {
                        characterMaster.inventory.GiveItem(GetRandomItem(ItemCatalog.tier3ItemList), 1);
                    }
                }

                if (_config.GiveLunarItems)
                {
                    for (int i = 0; i < itemCountTL; i++)
                    {
                        characterMaster.inventory.GiveItem(GetRandomItem(ItemCatalog.lunarItemList), 1);
                    }
                }
            }
            catch(Exception e)
            {
                Logger.LogError(e.Message + "      " + e.StackTrace);
            }
        }


        private void JoinAs(NetworkUser user, string characterName, string username)
        {
            if (!_config.SpawnAsEnabled)
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
                    Logger.LogWarning($"JoinAs :: Could not find specified player ({username})");
                    return;
                }
            }

            //Finding the body the player wants to spawn as.
            GameObject bodyPrefab = BodyHelper.FindBodyPrefab(characterName);

            //These are just to ensure that a null reference isn't thrown when we try to spawn our new player.
            if (!bodyPrefab)
            {
                AddChatMessage($"Couldn't find {characterName}, {player.userName}. Availible survivors are: {string.Join(", ", BodyHelper.GetSurvivorNames())}");
                //The character the player is trying to spawn as doesn't exist. 
                Logger.LogWarning("JoinAs :: Sent message to player informing them that what they requested to join as does not exist. Also bodyPrefab does not exist, returning!");
                return;
            }

            CharacterMaster characterMaster = player.master;
            if (characterMaster) //If the characterMaster exists.
            {
                //We have to check if the CharacterMaster is alive for these next two checks.
                bool hasBody = characterMaster.hasBody;
                if (hasBody)
                {
                    //If the characterMaster is alive, do stuff!
                    if (_config.AllowSpawnAsWhileAlive)
                    { //If the host has enabled this option, respawn the person using join_as.
                        characterMaster.bodyPrefab = bodyPrefab;
                        characterMaster.Respawn(characterMaster.GetBody().transform.position, characterMaster.GetBody().transform.rotation);
                        AddChatMessage(player.userName + " is respawning as " + Language.GetString(bodyPrefab.GetComponent<CharacterBody>().baseNameToken) + "!");
                    }
                    else if (!hasBody)
                    { //The player is dead, don't let them do anything.
                        AddChatMessage("Sorry " + player.userName + "! You can't use join_as while dead.");
                    }
                    else if (!_config.AllowSpawnAsWhileAlive && hasBody)
                    { //Host man has disabled this option, and the player is alive.
                        AddChatMessage("Sorry " + player.userName + "! The host has made it so you can't use join_as while alive.");
                    }
                }
            }
            else //Else, the person doesn't have a CharacterMaster. So we're gonna have to make the game setup the CharacterMaster for us.
            { 
                //Make sure the person can actually join. This allows SetupUserCharacterMaster (which is called in OnUserAdded) to work.
                Run.instance.SetFieldValue("allowNewParticipants", true);
                //Now that we've made sure the person can join, let's give them a CharacterMaster.
                Run.instance.OnUserAdded(user);

                //Cache the master after we actually give them one.
                CharacterMaster backupMaster = user.master;

                //CharacterMasters aren't made for each survivor, they all use the same master.
                //So we're gonna have to set the CharacterMaster's bodyPrefab to the prefab the person using join_as requested.
                backupMaster.bodyPrefab = bodyPrefab;

                //Offset for players so they don't spawn in the ground.
                Transform spawnTransform = Stage.instance.GetPlayerSpawnTransform();
                Vector3 posOffset = new Vector3(0, 1, 0);

                //Get our CharacterBody.
                CharacterBody body = backupMaster.SpawnBody(bodyPrefab, spawnTransform.position + posOffset, spawnTransform.rotation);
                //Now handle the player's enterance animation with our new CharacterBody.
                Run.instance.HandlePlayerFirstEntryAnimation(body, spawnTransform.position + posOffset, spawnTransform.rotation);

                //Inform the chat that the person using join_as is spawning as what they requested.
                AddChatMessage(player.userName + " is spawning as " + characterName + "!");

                //This makes it so the player will spawn in the next stage.
                if (!_config.ImmediateSpawn)
                {
                    Run.instance.SetFieldValue("allowNewParticipants", false);
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

        private void AddChatMessage(string message, float time = 0.1f) {
            StartCoroutine(AddHelperMessage(message, time));
        }

        private IEnumerator AddHelperMessage(string message, float time) {
            yield return new WaitForSeconds(time);
            var chatMessage = new Chat.SimpleChatMessage { baseToken = message };
            Chat.SendBroadcastChat(chatMessage);
        }
    }
}