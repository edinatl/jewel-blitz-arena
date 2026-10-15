using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Ownership gates which items may appear — not category spawn rates (pillar 3).
    /// </summary>
    public static class CrateEligibilityQuery
    {
        public static IReadOnlyList<int> EligibleItemIds(PlayerProfile profile, ContentCatalog? catalog)
        {
            var ids = new List<int>();
            if (catalog == null)
            {
                return ids;
            }

            foreach (ItemRecord item in catalog.Items)
            {
                if (item.CrateEligibility == CrateEligibility.Always)
                {
                    ids.Add(item.ItemId);
                    continue;
                }

                if (item.CrateEligibility == CrateEligibility.WhenOwned
                    && profile.Ledger.Owns(item.ItemId))
                {
                    ids.Add(item.ItemId);
                }
            }

            ids.Sort();
            return ids;
        }
    }
}
