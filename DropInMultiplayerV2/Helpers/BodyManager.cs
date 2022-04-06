using DropInMultiplayer;
using RoR2;
using System;
using System.Linq;

namespace DropInMultiplayer.Helpers
{
    internal class BodyManager
    {
        private readonly Run _run;
        private readonly DropInMultiplayerConfig _config;
        private static Random _rand = new Random();

        internal BodyManager(DropInMultiplayerConfig config, Run run)
        {
            _config = config;
            _run = run;
        }

        /// <summary>
        /// Gets array of availible surviors to join as for a player, based on run unlocks.
        /// 
        /// Code inspired by the <see cref="RoR2.CharacterMaster.PickRandomSurvivorBodyPrefab"/> method
        /// </summary>
        /// <param name="player">Player to get availble survivors for</param>
        /// <returns>Array of availble survivors</returns>
        public SurvivorDef[] GetAvailibleSurvivorsForPlayer(NetworkUser player)
        {
            return SurvivorCatalog.allSurvivorDefs.Where(SurvivorIsUnlockedAndAvailable).ToArray();
            
            bool SurvivorIsUnlockedAndAvailable(SurvivorDef survivorDef)
            {
                if (_config.AllowJoinAsHiddenSurvivors.Value || !survivorDef.hidden)
                {
                    if (!survivorDef.CheckRequiredExpansionEnabled())
                    {
                        return false;
                    }
                    UnlockableDef unlockableDef = survivorDef.unlockableDef;
                    return unlockableDef != null && _run.IsUnlockableUnlocked(unlockableDef);
                }
                return false;
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
        public SurvivorDef GetAvailibleSurvivorForPlayerByName(NetworkUser player, string name)
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
                return string.Equals(survivorDef.cachedName, name);
            }
        }
    }
}
