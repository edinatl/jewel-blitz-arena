using System;
using System.Collections.Generic;
using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Tilly's Bench Ledger room greybox (items-and-economy.md §3).
    /// Lists ever-owned catalog items; Pocket flags up to <see cref="PocketLoadout.MaxSlots"/>.
    /// </summary>
    public static class LedgerRuntime
    {
        public readonly struct LedgerEntry
        {
            public LedgerEntry(
                int itemId,
                string name,
                ItemCategory category,
                bool pocketable,
                bool isPocketed)
            {
                ItemId = itemId;
                Name = name;
                Category = category;
                Pocketable = pocketable;
                IsPocketed = isPocketed;
            }

            public int ItemId { get; }
            public string Name { get; }
            public ItemCategory Category { get; }
            public bool Pocketable { get; }
            public bool IsPocketed { get; }
        }

        public static IReadOnlyList<LedgerEntry> BuildEntries(
            ContentCatalog catalog,
            PlayerProfile profile)
        {
            var result = new List<LedgerEntry>();
            if (catalog == null || profile == null)
            {
                return result;
            }

            int[] owned = profile.Ledger.ToArray();
            for (int i = 0; i < owned.Length; i++)
            {
                if (!catalog.TryGetItem(owned[i], out ItemRecord item))
                {
                    continue;
                }

                result.Add(new LedgerEntry(
                    item.ItemId,
                    item.Name,
                    item.Category,
                    item.Pocketable,
                    profile.Pockets.Contains(item.ItemId)));
            }

            return result;
        }

        public static IReadOnlyList<LedgerEntry> OpenLedger(
            PlayerProfile profile,
            ContentCatalog catalog)
        {
            if (profile == null || catalog == null)
            {
                return Array.Empty<LedgerEntry>();
            }

            profile.SetFlag(ProfileFlags.TouchedLedgerRoom);
            return BuildEntries(catalog, profile);
        }

        /// <summary>Flag an owned pocketable item into a Pocket slot (max 5).</summary>
        public static bool TryPocket(
            PlayerProfile profile,
            ContentCatalog catalog,
            int itemId)
        {
            if (profile == null || catalog == null
                || !profile.Ledger.Owns(itemId)
                || !catalog.TryGetItem(itemId, out ItemRecord item)
                || !item.Pocketable)
            {
                return false;
            }

            return profile.Pockets.TryAdd(itemId);
        }

        public static bool TryUnpocket(PlayerProfile profile, int itemId) =>
            profile != null && profile.Pockets.TryRemove(itemId);
    }
}
