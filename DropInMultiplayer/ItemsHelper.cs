using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DropInMultiplayer
{
    internal static class ItemsHelper
    {
        private static readonly Random _rand = new Random();

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

            AddToItemsToMatch(targetInventory, otherPlayerInventories, Run.instance.availableTier1DropList.Select(item => PickupCatalog.GetPickupDef(item).itemIndex), ItemTier.Tier1);
            AddToItemsToMatch(targetInventory, otherPlayerInventories, Run.instance.availableTier2DropList.Select(item => PickupCatalog.GetPickupDef(item).itemIndex), ItemTier.Tier2);
            if (includeRed)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, Run.instance.availableTier3DropList.Select(item => PickupCatalog.GetPickupDef(item).itemIndex), ItemTier.Tier3);
            }
            if (includeLunar)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, Run.instance.availableLunarDropList.Select(item => PickupCatalog.GetPickupDef(item).itemIndex), ItemTier.Lunar);
            }
            if (includeBoss)
            {
                AddToItemsToMatch(targetInventory, otherPlayerInventories, Run.instance.availableBossDropList.Select(item => PickupCatalog.GetPickupDef(item).itemIndex), ItemTier.Boss);
            }
        }

        private static void AddToItemsToMatch(Inventory targetInventory, Inventory[] otherPlayerInventories, IEnumerable<ItemIndex> itemsInTier, ItemTier itemTier)
        {
            var difference = (int)otherPlayerInventories.Average(inv => GetItemCountWithExclusions(inv, itemsInTier, itemTier)) - GetItemCountWithExclusions(targetInventory, itemsInTier, itemTier);
            for (int i = 0; i < difference; i++)
            {
                targetInventory.GiveItem(itemsInTier.ElementAt(_rand.Next(itemsInTier.Count())), 1);
            }
        }

        private static int GetItemCountWithExclusions(Inventory inventory, IEnumerable<ItemIndex> itemsInTier, ItemTier itemTier)
        {
            var validCountItems = itemsInTier.ToList();
            switch (itemTier)
            {
                case ItemTier.Tier1:
                    validCountItems.Add(RoR2Content.Items.ScrapWhite.itemIndex);
                    break;
                case ItemTier.Tier2:
                    validCountItems.Add(RoR2Content.Items.ScrapGreen.itemIndex);
                    break;
                case ItemTier.Tier3:
                    validCountItems.Add(RoR2Content.Items.ScrapRed.itemIndex);
                    break;
                case ItemTier.Boss:
                    validCountItems.Add(RoR2Content.Items.ScrapYellow.itemIndex);
                    break;
            }

            return validCountItems.Sum(inventory.GetItemCount);
        }
    }
}
