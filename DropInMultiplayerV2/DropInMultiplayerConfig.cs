using BepInEx.Configuration;
using System;
using BepInEx;
using System.Linq;

namespace DropInMultiplayer
{
    public class DropInMultiplayerConfig
    {
        private const string DefaultWelcomeMessage = "Hello {username}! Join the game by typing '/join_as {survivor name}' in chat (or '/join_as random'). To get a list of availible survivors, type '/list_survivors' in chat";

        // General
        public ConfigEntry<bool> AllowRespawn { get; }
        public ConfigEntry<bool> JoinAsRandomByDefault { get; }

        // Survivors
        public ConfigEntry<bool> AllowJoinAsHiddenSurvivors { get; }
        public ConfigEntry<bool> AllowJoinAsAllBodies { get; }
        public ConfigEntry<bool> GiveHereticItems { get; }  
        public ConfigEntry<bool> PreventCaptainScrapAbuse { get; }
        public ConfigEntry<bool> PreventOperatorDroneAbuse { get; }

        // Items
        public ConfigEntry<bool> GiveCatchUpItems { get; }
        public ConfigEntry<bool> GiveRejoiningPlayersCatchUpItems { get; }
        public ConfigEntry<bool> GiveCatchUpVoidItems { get; }
        public ConfigEntry<bool> GiveCatchUpLunarItems { get; }

        // Chat Messages
        public ConfigEntry<bool> SendWelcomeMessage { get; }
        public ConfigEntry<string> CustomWelcomeMessage { get; }

        public DropInMultiplayerConfig(ConfigFile config)
        {
            // General
            AllowRespawn = config.Bind("General", "AllowRespawn", true, "When enabled dead players who use the join as command will be immediately respawned");
            JoinAsRandomByDefault = config.Bind("General", "JoinAsRandomByDefault", false, "When enabled newly joined players will be spawned as random survivor by default");

            // Survivors
            AllowJoinAsHiddenSurvivors = config.Bind("Survivors", "AllowJoinAsHiddenSurvivors", true, "When enabled allows players to join as hidden characters, e.g. heretic");
            AllowJoinAsAllBodies = config.Bind("General", "AllowJoinAsAllBodies", false, "When enabled using the join as command will attempt to match with any body, e.g. join_as beetle, WARNING: Very Untested!");
            GiveHereticItems = config.Bind("Survivors", "GiveHereticItems", true, "When enabled joining as Heretic will give all 4 Heretic items automatically.");
            PreventCaptainScrapAbuse = config.Bind("Survivors", "PreventCaptainScrapAbuse", true, "When enabled Captain will not receive replacement Microbots if it was scrapped or removed.");

            // Items
            GiveCatchUpItems = config.Bind("Items", "GiveCatchUpItems", true, "When enabled players will be given catch up items when joining");
            GiveRejoiningPlayersCatchUpItems = config.Bind("Items", "GiveRejoiningPlayersCatchUpItems", true, "When enabled players who leave and rejoin will be given catchup items, WARNING: Can be exploited by giving all items to one player then leaving and rejoining");
            GiveCatchUpVoidItems = config.Bind("Items", "GiveCatchUpVoidItems", false, "When enabled lunar items will be dropped when players start (requires GiveCatchUpItems enabled)");
            GiveCatchUpLunarItems = config.Bind("Items", "GiveCatchUpLunarItems", false, "When enabled lunar items will be dropped when players start (requires GiveCatchUpItems enabled)");

            // Chat Messages
            SendWelcomeMessage = config.Bind("Chat Messages", "SendWelcomeMessage", true, "Sends the welcome message when a new player joins.");
            CustomWelcomeMessage = config.Bind("Chat Messages", "CustomWelcomeMessage", DefaultWelcomeMessage,
                "Format of welcome message. {username} will be replaced with joining users name, and {survivorlist} will be replaced by list of availible survivors.");
        }
    }
}
