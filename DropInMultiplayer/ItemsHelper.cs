using BepInEx.Logging;
using RoR2;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DropInMultiplayer
{
    public static class ItemsHelper
    {
        private static List<ItemIndex> _bossItems;
        private static List<ItemIndex> _validT3Items;
        private static List<ItemIndex> _invalidT3Items = new List<ItemIndex> { ItemIndex.CaptainDefenseMatrix, ItemIndex.ScrapRed };
        private static List<ItemIndex> _allInvalidItems = _invalidT3Items.Union(new List<ItemIndex>() { ItemIndex.Pearl, ItemIndex.ShinyPearl, ItemIndex.TitanGoldDuringTP, ItemIndex.ArtifactKey, ItemIndex.ScrapYellow, ItemIndex.ScrapWhite, ItemIndex.ScrapGreen }).ToList();
        static ItemsHelper()
        {
            On.RoR2.ItemCatalog.Init += AddBossItems;
        }

        private static void AddBossItems(On.RoR2.ItemCatalog.orig_Init orig_Init)
        {
            orig_Init();
            
        }

        private static ItemIndex GetRandomItem(IList<ItemIndex> items)
        { 
            return items[UnityEngine.Random.Range(0, items.Count())];
        }

        public static void CopyItemsFromRandom(NetworkUser joiningPlayer)
        {
            var characterMaster = joiningPlayer.master;
            var otherPlayers = NetworkUser.readOnlyInstancesList
                .Where(player => !player.id.Equals(joiningPlayer.id) && player.master != null) // Don't include self or any other players who don't have a character
                .ToArray();

            if (characterMaster == null || // The new player does not have character yet
                otherPlayers.Length <= 0) // We are the only player with a character
            {
                return;
            }

            var copyFrom = otherPlayers[UnityEngine.Random.Range(0, otherPlayers.Length)];
            joiningPlayer.master.inventory.CopyItemsFrom(copyFrom.master.inventory);
        }
        
        private static int GetItemCountWithExclusions(Inventory inventory, ItemTier itemTier)
        {
            var count = inventory.GetTotalItemCountOfTier(itemTier);
            // We don't want to count any instances of the captain's red item, since this is innate
            if (itemTier == ItemTier.Tier3)
            {
                count -= inventory.GetItemCount(ItemIndex.CaptainDefenseMatrix);
            }
            return count;
        }

        private static void AddToItemsToMatch(Inventory targetInventory, Inventory[] otherPlayerInventories, List<ItemIndex> itemTierList, ItemTier itemTier)
        {
            var filteredList = itemTierList.Except(_allInvalidItems).ToArray();
            var difference =  (int) otherPlayerInventories.Average(inv => GetItemCountWithExclusions(inv, itemTier)) - GetItemCountWithExclusions(targetInventory, itemTier);
            for (int i = 0; i < difference; i++)
            {
                targetInventory.GiveItem(GetRandomItem(filteredList), 1);
            }
        }

        public static void GiveAveragedItems(NetworkUser joiningPlayer, bool includeRed, bool includeLunar, bool includeBoss)
        {
            var targetInventory = joiningPlayer?.master?.inventory;
            var otherPlayerInventories = NetworkUser.readOnlyInstancesList
                .Where(player => !player.id.Equals(joiningPlayer.id) && player?.master?.inventory != null) // Don't include self or any other players who don't have a character
                .Select(p => p.master.inventory)
                .ToArray();
            
            if (targetInventory == null || // The new player does not have character yet
                otherPlayerInventories.Length <= 0) // We are the only player
            {
                return;
            }

            AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemCatalog.tier1ItemList, ItemTier.Tier1);
            AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemCatalog.tier2ItemList, ItemTier.Tier2);
            if (includeRed)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemCatalog.tier3ItemList, ItemTier.Tier3);
            }
            if (includeLunar)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemCatalog.lunarItemList, ItemTier.Lunar);
            }
            if (includeBoss)
            { 
                if (_bossItems == null)
                {
                    _bossItems = ItemCatalog.allItems.Select(idx => ItemCatalog.GetItemDef(idx)).Where(item => item.tier == ItemTier.Boss).Select(item => item.itemIndex).ToList();
                }
                AddToItemsToMatch(targetInventory, otherPlayerInventories, _bossItems, ItemTier.Boss);
            }
        }
    }
}
