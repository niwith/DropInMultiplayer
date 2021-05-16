using RoR2;
using System.Collections.Generic;
using System.Linq;

namespace DropInMultiplayer
{
    internal static class ItemsHelper
    {
        private static readonly HashSet<ItemIndex> _allInvalidDropItems = new HashSet<ItemIndex>() { 
            RoR2Content.Items.CaptainDefenseMatrix.itemIndex, // Character unique items
            RoR2Content.Items.Pearl.itemIndex, RoR2Content.Items.ShinyPearl.itemIndex, // Pearls
            RoR2Content.Items.TitanGoldDuringTP.itemIndex, RoR2Content.Items.ArtifactKey.itemIndex, 
            RoR2Content.Items.InvadingDoppelganger.itemIndex, // Honestly don't know what this is, does it cause the doppleganger to spawn?
            RoR2Content.Items.ScrapYellow.itemIndex, RoR2Content.Items.ScrapWhite.itemIndex, RoR2Content.Items.ScrapGreen.itemIndex, RoR2Content.Items.ScrapRed.itemIndex // Scap
        };
        private static readonly HashSet<ItemIndex> _allInvalidCountItems = new HashSet<ItemIndex>() { RoR2Content.Items.CaptainDefenseMatrix.itemIndex };

        private static List<ItemIndex> _bossItems; // cache for boss items (boss items initialised later)
        
        internal static void AddInvalidItems(IEnumerable<ItemIndex> items, bool alsoInvalidForCount = false)
        {
            foreach (var item in items)
            {
                _allInvalidDropItems.Add(item);
            }

            if (alsoInvalidForCount)
            {
                foreach (var item in items)
                {
                    _allInvalidCountItems.Add(item);
                }
            }
        }

        internal static void CopyItemsFromRandom(NetworkUser joiningPlayer)
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

        internal static void GiveAveragedItems(NetworkUser joiningPlayer, bool includeRed, bool includeLunar, bool includeBoss)
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

        private static void AddToItemsToMatch(Inventory targetInventory, Inventory[] otherPlayerInventories, List<ItemIndex> itemTierList, ItemTier itemTier)
        {
            var filteredList = itemTierList.Except(_allInvalidDropItems).ToArray();
            var difference = (int)otherPlayerInventories.Average(inv => GetItemCountWithExclusions(inv, itemTier)) - GetItemCountWithExclusions(targetInventory, itemTier);
            for (int i = 0; i < difference; i++)
            {
                targetInventory.GiveItem(GetRandomItem(filteredList), 1);
            }
        }

        private static int GetItemCountWithExclusions(Inventory inventory, ItemTier itemTier)
        {
            return inventory.GetTotalItemCountOfTierWithExclusions(itemTier, _allInvalidCountItems);
        }

        private static ItemIndex GetRandomItem(IList<ItemIndex> items)
        {
            return items[UnityEngine.Random.Range(0, items.Count())];
        }
    }

    internal static class InventoryExtensions
    {
        internal static int GetTotalItemCountOfTierWithExclusions(this Inventory inventory, ItemTier tier, IEnumerable<ItemIndex> exclusions)
        {
            var exclusionsOfTier = exclusions
                .Select(e => ItemCatalog.GetItemDef(e))
                .Where(i => i.tier == tier)
                .Select(i => i.itemIndex);

            var itemsInTier = inventory.GetTotalItemCountOfTier(tier);

            var excludedOfTierInInv = exclusionsOfTier.Sum(inventory.GetItemCount);

            return itemsInTier - excludedOfTierInInv;
        }
    }
}
