using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DropInMultiplayer
{
    public static class BodyHelper
    {
        public static IEnumerable<SurvivorDef> _survivorBodies;

        public static IEnumerable<SurvivorDef> SurvivorBodies { 
            get
            {
                if (_survivorBodies == null)
                {
                    _survivorBodies = SurvivorCatalog.allSurvivorDefs.Where(def => def.bodyPrefab).ToArray();
                }
                return _survivorBodies;
            }
        }

        private static bool CompareNameStringsNoSpaces(string compareFrom, string compareTo)
        {
            compareFrom = compareFrom.Replace(" ", string.Empty);
            compareTo = compareTo.Replace(" ", string.Empty);
            return compareFrom.Equals(compareTo, StringComparison.InvariantCultureIgnoreCase);
        }

        public static SurvivorDef LookupSurvior(string name)
        {
            foreach (var survivor in SurvivorBodies)
            {
                var nameEqual = survivor.name != null && CompareNameStringsNoSpaces(survivor.name, name);
                var displayNameEqual = survivor.displayNameToken != null && CompareNameStringsNoSpaces(Language.GetString(survivor.displayNameToken), name);
                if (nameEqual || displayNameEqual)
                {
                    return survivor;
                }
            }

            return null;
        }

        public static IEnumerable<string> GetSurvivorDisplayNames()
        {
            return SurvivorBodies.Select(def => Language.GetString(def.displayNameToken).Replace(" ", string.Empty));
        }

        public static GameObject FindBodyPrefab(string characterName)
        {
            return LookupSurvior(characterName)?.bodyPrefab;
        }
    }
}
