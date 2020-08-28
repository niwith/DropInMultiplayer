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

        public static SurvivorDef LookupSurvior(string name)
        {
            foreach (var survivor in SurvivorBodies)
            {
                if (survivor.name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return survivor;
                }
            }

            return null;
        }

        public static IEnumerable<string> GetSurvivorNames()
        {
            return SurvivorBodies.Select(def => def.name);
        }

        public static GameObject FindBodyPrefab(string characterName)
        {
            foreach (var survivorDef in SurvivorBodies)
            {
                if (survivorDef.name.Equals(characterName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return survivorDef.bodyPrefab;
                }
            }

            return null;
        }
    }
}
