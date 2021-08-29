using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DropInMultiplayer
{
    internal static class ItemsHelper
    {
        private static readonly System.Random _rand = new System.Random();

        private static readonly Dictionary<ItemTier, IEnumerable<ItemIndex>> _validCountItemsCache = new Dictionary<ItemTier, IEnumerable<ItemIndex>>();
        private static readonly Dictionary<ItemTier, IEnumerable<ItemIndex>> _validDropItemsCache = new Dictionary<ItemTier, IEnumerable<ItemIndex>>();

        private static readonly ItemTier[] _itemTiers = ((ItemTier[]) Enum.GetValues(typeof(ItemTier))).Except(new[] { ItemTier.NoTier }).ToArray();
        public static ItemTier[] ItemTiers => _itemTiers;

        public static IEnumerable<PickupIndex> GetDropListForTier(ItemTier itemTier, bool addTierScrap = false)
        {
            IEnumerable<PickupIndex> HandleWithScrap(IEnumerable<PickupIndex> baseList, ItemDef scrap)
            {
                return addTierScrap ? baseList.Append(PickupCatalog.FindPickupIndex(scrap.itemIndex)) : baseList;
            }

            switch (itemTier)
            {
                case ItemTier.Tier1:
                    return HandleWithScrap(Run.instance.availableTier1DropList, RoR2Content.Items.ScrapWhite);
                case ItemTier.Tier2:
                    return HandleWithScrap(Run.instance.availableTier2DropList, RoR2Content.Items.ScrapGreen);
                case ItemTier.Tier3:
                    return HandleWithScrap(Run.instance.availableTier3DropList, RoR2Content.Items.ScrapRed);
                case ItemTier.Boss:
                    return HandleWithScrap(Run.instance.availableBossDropList, RoR2Content.Items.ScrapYellow);
                case ItemTier.Lunar:
                    return Run.instance.availableLunarDropList;
                default:
                    throw new Exception($"ItemTier {itemTier} has not been handled");
            }
        }

        private static void RefreshItemCaches()
        {
            // I don't love this, but doing with loops would probably be more dodgy
            void RefreshCache(Dictionary<ItemTier, IEnumerable<ItemIndex>> cache, IEnumerable<string> exceptNames, bool includeScrap = false)
            {
                IEnumerable<ItemIndex> GetItemIndexesWithExceptions(IEnumerable<PickupIndex> pickupIndices)
                {
                    return pickupIndices.Select(pickupIndex => PickupCatalog.GetPickupDef(pickupIndex).itemIndex).Except(GetItemIndicesByNames(exceptNames));
                }

                foreach(var tier in _itemTiers)
                {
                    cache[tier] = GetItemIndexesWithExceptions(GetDropListForTier(tier, includeScrap)).ToArray();
                }
            }

            RefreshCache(_validDropItemsCache, DropInMultiplayer.Instance.DropInConfig.DropItemsBlackList);
            RefreshCache(_validCountItemsCache, DropInMultiplayer.Instance.DropInConfig.CountItemsBlackList, true);
        }

        private static IEnumerable<ItemIndex> GetValidCountItems(ItemTier tier)
        {
            return _validCountItemsCache[tier];
        }

        private static IEnumerable<ItemIndex> GetValidDropItems(ItemTier tier)
        {
            return _validDropItemsCache[tier];
        }


        [Obsolete("No longer required, items to be dropped are now pulled dynamically from run instance")]
        internal static void AddInvalidItems(IEnumerable<ItemIndex> items, bool alsoInvalidForCount = false)
        {
            return;
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
            RefreshItemCaches(); // Not sure if drop lists can change during a run, so refresh the cache each time a player joins just incase

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

            AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemTier.Tier1);
            AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemTier.Tier2);
            if (includeRed)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemTier.Tier3);
            }
            if (includeLunar)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemTier.Lunar);
            }
            if (includeBoss)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, ItemTier.Boss);
            }
        }

        private static IEnumerable<ItemIndex> GetItemIndicesByNames(IEnumerable<string> itemNames)
        {
            if (itemNames == null || itemNames.Count() == 0)
            {
                return new ItemIndex[0];
            }

            var itemIndexes = new List<ItemIndex>();
            foreach (var itemName in itemNames)
            {
                // Language thing makes it easier to make the list using display names, or use internal name
                // to make the list transferable between different language configurations
                var pickupDef = PickupCatalog.allPickups.FirstOrDefault(pickup => 
                        pickup.internalName.Equals("ItemIndex." + itemName) || 
                        // Replace commas because we need them for comma seperated values
                        Language.GetString(pickup.nameToken).Replace(",", "").Equals(itemName, StringComparison.InvariantCultureIgnoreCase));

                if (pickupDef == null || !pickupDef.pickupIndex.isValid || pickupDef.pickupIndex == PickupIndex.none)
                {
                    Debug.LogWarning($"Could not find item with name {itemName} to blacklist");
                    continue;
                }
                itemIndexes.Add(pickupDef.itemIndex);
            }

            return itemIndexes;
        }

        private static void AddToItemsToMatch(Inventory targetInventory, Inventory[] otherPlayerInventories, ItemTier itemTier)
        {
            var validDropItems = GetValidDropItems(itemTier);
            
            // Average of other player inventory count for this tier, minus target players current count in the tier
            var difference = (int)otherPlayerInventories.Average(inv => GetItemCountWithExclusions(inv, itemTier)) - GetItemCountWithExclusions(targetInventory, itemTier);
            
            for (int i = 0; i < difference; i++)
            {
                GiveRandItemFromList(targetInventory, validDropItems);
            }
        }

        private static void GiveRandItemFromList(Inventory targetInventory, IEnumerable<ItemIndex> items)
        {
            // For whatever reason if we have blacklisted every item in this tier just return without breaking anything
            if (items.Count() == 0)
            {
                return;
            }
            targetInventory.GiveItem(items.ElementAt(_rand.Next(items.Count())), 1);
        }

        private static int GetItemCountWithExclusions(Inventory inventory, ItemTier itemTier)
        {
            var validCountItems = GetValidCountItems(itemTier);
            return validCountItems.Sum(inventory.GetItemCount);
        }
    }
}
