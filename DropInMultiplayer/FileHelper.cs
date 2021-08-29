using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DropInMultiplayer
{
    public static class FileHelper
    {
        public static string LogDropItemsToFile(string baseFolderPath = null)
        {
            baseFolderPath = baseFolderPath ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RiskOfRain2Items");

            Directory.CreateDirectory(baseFolderPath);

            foreach (var itemTier in ItemsHelper.ItemTiers)
            {
                var items = ItemsHelper.GetDropListForTier(itemTier, true);
                var builder = new StringBuilder();
                builder.AppendLine("Display Name,Internal Name");
                foreach (var item in items)
                {
                    var pickupDef = PickupCatalog.GetPickupDef(item);
                    // Removing ItemIndex. because its automatically added, and removing commas because we need commas for comma seperated values
                    builder.AppendLine($"{Language.GetString(pickupDef.nameToken).Replace(",","")},{pickupDef.internalName.Replace("ItemIndex.", "")}");
                }
                File.WriteAllText(System.IO.Path.Combine(baseFolderPath, $"{itemTier}.csv"), builder.ToString());
            }

            return baseFolderPath;
        }
    }
}
