using BepInEx.Configuration;
using System;
using BepInEx;

namespace DropInMultiplayer
{
    public class DropInMultiplayerConfig
    {
        private readonly ConfigEntry<bool> _joinAsEnabled;
        private readonly ConfigEntry<bool> _hostOnlySpawnAs;
        private readonly ConfigEntry<bool> _allowReJoinAs;
        
        private readonly ConfigEntry<bool> _startWithItems;
        private readonly ConfigEntry<bool> _giveExactItems;
        private readonly ConfigEntry<bool> _giveRedItems;
        private readonly ConfigEntry<bool> _giveLunarItems;
        private readonly ConfigEntry<bool> _giveBossItems;
     
        private readonly ConfigEntry<bool> _welcomeMessage;

        public bool JoinAsEnabled { get => _joinAsEnabled.Value; }
        public bool HostOnlySpawnAs { get => _hostOnlySpawnAs.Value; }
        public bool AllowReJoinAs { get => _allowReJoinAs.Value; }
        
        public bool StartWithItems { get => _startWithItems.Value; }
        public bool GiveExactItems { get => _giveExactItems.Value; }
        public bool GiveRedItems { get => _giveRedItems.Value; }
        public bool GiveLunarItems { get => _giveLunarItems.Value; }
        public bool GiveBossItems { get => _giveBossItems.Value; }

        public bool WelcomeMessage { get => _welcomeMessage.Value; }

        public DropInMultiplayerConfig(ConfigFile config)
        {
            _joinAsEnabled = config.Bind("Enable/Disable", "Join_As", true, "Enables or disables the join_as command.");
            _hostOnlySpawnAs = config.Bind("Enable/Disable", "HostOnlyJoin_As", false, "Changes the join_as command to be host only");
            _allowReJoinAs = config.Bind("Enable/Disable", "AllowReJoin_As", false, "When enabled, allows players to use the join_as command after they have already selected a character");

            _startWithItems = config.Bind("Enable/Disable", "StartWithItems", true, "Enables or disables giving players items if they join mid-game.");
            _giveExactItems = config.Bind("Enable/Disable", "GiveExactItems", false, "Chooses a random member in the game and gives the new player their items, should be used with ShareSuite.");
            _giveRedItems = config.Bind("Enable/Disable", "GiveRedItems", true, "Allows red items to be given to players, needs StartWithItems to be enabled!");
            _giveLunarItems = config.Bind("Enable/Disable", "GiveLunarItems", false, "Allows lunar items to be given to players, needs StartWithItems to be enabled!");
            _giveBossItems = config.Bind("Enable/Disable", "GiveBossItems", true, "Allows boss items to be given to players, needs StartWithItems to be enabled!");
            
            _welcomeMessage = config.Bind("Enable/Disable", "WelcomeMessage", true, "Sends the welcome message when a new player joins.");
        }
    }
}
