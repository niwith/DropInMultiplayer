using BepInEx.Configuration;
using System;
using BepInEx;

namespace DropInMultiplayer
{
    public class DropInMultiplayerConfig
    {
        private readonly ConfigEntry<bool> _immediateSpawn;
        private readonly ConfigEntry<bool> _giveRedItems;
        private readonly ConfigEntry<bool> _giveLunarItems;
        private readonly ConfigEntry<bool> _hostOnlySpawnAs;
        private readonly ConfigEntry<bool> _spawnAsEnabled;
        private readonly ConfigEntry<bool> _startWithItems;
        private readonly ConfigEntry<bool> _allowSpawnAsWhileAlive;
        private readonly ConfigEntry<bool> _welcomeMessage;
        private readonly ConfigEntry<bool> _giveExactItems;

        public bool ImmediateSpawn { get => _immediateSpawn.Value; }
        public bool AllowSpawnAsWhileAlive { get => _allowSpawnAsWhileAlive.Value; }
        public bool StartWithItems { get => _startWithItems.Value; }
        public bool SpawnAsEnabled { get => _spawnAsEnabled.Value; }
        public bool HostOnlySpawnAs { get => _hostOnlySpawnAs.Value; }
        public bool GiveLunarItems { get => _giveLunarItems.Value; }
        public bool GiveRedItems { get => _giveRedItems.Value; }
        public bool WelcomeMessage { get => _welcomeMessage.Value; }
        public bool GiveExactItems { get => _giveExactItems.Value; }

        public DropInMultiplayerConfig(ConfigFile config)
        {
            _immediateSpawn = config.Bind("Enable/Disable", "ImmediateSpawn", false, "Enables or disables immediate spawning as you join");
            _startWithItems = config.Bind("Enable/Disable", "StartWithItems", true, "Enables or disables giving players items if they join mid-game");
            _allowSpawnAsWhileAlive = config.Bind("Enable/Disable", "AllowJoinAsWhileAlive", false, "Enables or disables players using join_as while alive");
            _spawnAsEnabled = config.Bind("Enable/Disable", "Join_As", true, "Enables or disables the join_as command");
            _hostOnlySpawnAs = config.Bind("Enable/Disable", "HostOnlyJoin_As", false, "Changes the join_as command to be host only");
            _giveLunarItems = config.Bind("Enable/Disable", "GiveLunarItems", false, "Allows lunar items to be given to players, needs StartWithItems to be enabled!");
            _giveRedItems = config.Bind("Enable/Disable", "GiveRedItems", true, "Allows red items to be given to players, needs StartWithItems to be enabled!");
            _welcomeMessage = config.Bind("Enable/Disable", "WelcomeMessage", true, "Sends the welcome message when a new player joins.");
            _giveExactItems = config.Bind("Enable/Disable", "GiveExactItems", false, "Chooses a random member in the game and gives the new player their items, should be used with ShareSuite, needs StartWithItems to be enabled, and also disables GiveRedItems, GiveLunarItems!");
        }
    }
}
