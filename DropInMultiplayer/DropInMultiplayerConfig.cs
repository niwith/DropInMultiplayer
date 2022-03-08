using BepInEx.Configuration;
using System;
using BepInEx;
using System.Linq;

namespace DropInMultiplayer
{
    public class DropInMultiplayerConfig
    {
        private const string DefaultWelcomeMessage = "Hello {username}! Join the game by typing 'join_as [character name]' in chat. Available survivors are: {survivorlist} (or use Random)";

        private readonly ConfigEntry<bool> _joinAsEnabled;
        private readonly ConfigEntry<bool> _hostOnlySpawnAs;
        private readonly ConfigEntry<bool> _allowReJoinAs;
        private readonly ConfigEntry<bool> _allowJoinAsHeretic;

        private readonly ConfigEntry<bool> _allowMoonJoinAs;

        private readonly ConfigEntry<bool> _startWithItems;
        private readonly ConfigEntry<bool> _giveExactItems;
        private readonly ConfigEntry<bool> _giveRedItems;
        private readonly ConfigEntry<bool> _giveLunarItems;
        private readonly ConfigEntry<bool> _giveBossItems;
        private readonly ConfigEntry<string> _dropItemsBlackList;
        private readonly ConfigEntry<string> _countItemsBlackList;

        private readonly ConfigEntry<bool> _welcomeMessage;
        private readonly ConfigEntry<string> _customWelcomeMessage;

        public bool JoinAsEnabled { get => _joinAsEnabled.Value; }
        public bool HostOnlySpawnAs { get => _hostOnlySpawnAs.Value; }
        public bool AllowReJoinAs { get => _allowReJoinAs.Value; }

        public bool AllowMoonDropIn { get => _allowMoonJoinAs.Value; }

        public bool AllowJoinAsHeretic { get => _allowJoinAsHeretic.Value; }

        public bool StartWithItems { get => _startWithItems.Value; }
        public bool GiveExactItems { get => _giveExactItems.Value; }
        public bool GiveRedItems { get => _giveRedItems.Value; }
        public bool GiveLunarItems { get => _giveLunarItems.Value; }
        public bool GiveBossItems { get => _giveBossItems.Value; }
        private string[] _dropItemsBlackListParsed = null;
        public string[] DropItemsBlackList
        {
            get
            {
                if (_dropItemsBlackListParsed == null)
                {
                    _dropItemsBlackListParsed = _dropItemsBlackList.Value.Split(',')
                        .Where(entry => entry.Length != 0)
                        .Select(entry => entry.Trim())
                        .ToArray();
                }
                return _dropItemsBlackListParsed;
            }
        }
        private string[] _countItemsBlackListParsed = null;
        public string[] CountItemsBlackList
        {
            get
            {
                if (_countItemsBlackListParsed == null)
                {
                    _countItemsBlackListParsed = _countItemsBlackList.Value.Split(',')
                        .Where(entry => entry.Length != 0)
                        .Select(entry => entry.Trim())
                        .ToArray();
                }
                return _countItemsBlackListParsed;
            }
        }

        public bool WelcomeMessage { get => _welcomeMessage.Value; }
        public string CustomWelcomeMessage { get => _customWelcomeMessage.Value; }

        public DropInMultiplayerConfig(ConfigFile config)
        {            
            // General
            _joinAsEnabled = config.Bind("Enable/Disable", "Join_As", true, "Enables or disables the join_as command.");
            _hostOnlySpawnAs = config.Bind("Enable/Disable", "HostOnlyJoin_As", false, "Changes the join_as command to be host only");
            _allowReJoinAs = config.Bind("Enable/Disable", "AllowReJoin_As", false, "When enabled, allows players to use the join_as command after they have already selected a character");
            
            // Levels
            _allowMoonJoinAs = config.Bind("Levels", "AllowMoonJoinAs", false, "Enabling dropin on the final stage (moon) can cause softlock, enable at your own risk");

            // Survivors
            _allowJoinAsHeretic = config.Bind("Enable/Disable", "AllowJoinAsHeretic", false, "When enabled, allows players to join as the heretic, and will be given the items for the heretic");

            // Items
            _startWithItems = config.Bind("Enable/Disable", "StartWithItems", true, "Enables or disables giving players items if they join mid-game.");
            _giveExactItems = config.Bind("Enable/Disable", "GiveExactItems", false, "Chooses a random member in the game and gives the new player their items, should be used with ShareSuite.");
            _giveRedItems = config.Bind("Enable/Disable", "GiveRedItems", true, "Allows red items to be given to players, needs StartWithItems to be enabled!");
            _giveLunarItems = config.Bind("Enable/Disable", "GiveLunarItems", false, "Allows lunar items to be given to players, needs StartWithItems to be enabled!");
            _giveBossItems = config.Bind("Enable/Disable", "GiveBossItems", true, "Allows boss items to be given to players, needs StartWithItems to be enabled!");
            _dropItemsBlackList = config.Bind("Items", "DropItemsBlacklist", "", "Items in this list will not be dropped for players when joining (comma seperated, no spaces)");
            _countItemsBlackList = config.Bind("Items", "CountItemsBlackList", "", "Items in this list will not be counted for number of items to drop for players when joining (comma seperated, no spaces)");

            // Welcome Message
            _welcomeMessage = config.Bind("Enable/Disable", "WelcomeMessage", true, "Sends the welcome message when a new player joins.");
            _customWelcomeMessage = config.Bind("Welcome Message", "CustomWelcomeMessage", DefaultWelcomeMessage, 
                "Format of welcome message. {username} will be replaced with joining users name, and {survivorlist} will be replaced by list of availible survivors.");

            // MigrateLegacySettings(config);
        }

        //private void MigrateLegacySettings(ConfigFile config)
        //{
        //    // TODO: requires features from this bepinex pull request, until then config will be wierd https://github.com/BepInEx/BepInEx/pull/267
        //}
    }
}
