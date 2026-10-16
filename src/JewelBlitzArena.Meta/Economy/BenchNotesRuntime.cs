using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Bench Notes recording + Logbook give stubs (items-and-economy.md §4).
    /// Logbook item ids align with S5 Card stubs (<see cref="SampleContentFactory.LogbookItemIdMin"/>).
    /// </summary>
    public static class BenchNotesRuntime
    {
        public const int LogbookItemIdMin = SampleContentFactory.LogbookItemIdMin;
        public const int LogbookItemIdMax = SampleContentFactory.LogbookItemIdMax;
        public const int LogbookCount = SampleContentFactory.LogbookCount;

        public static bool IsLogbookItemId(int itemId) =>
            itemId >= LogbookItemIdMin && itemId <= LogbookItemIdMax;

        public static void RecordCraft(
            PlayerProfile profile,
            int inputA,
            int inputB,
            TinkerOutcome outcome)
        {
            if (profile == null)
            {
                return;
            }

            profile.BenchNotes.RecordAttempt(inputA, inputB, outcome);
        }

        /// <summary>
        /// Give a Logbook to Tilly: consume from Ledger and reveal the pair's full pool.
        /// Returns false if not a logbook, not owned, or already revealed.
        /// </summary>
        public static bool TryGiveLogbook(
            PlayerProfile profile,
            int logbookItemId,
            int pairA,
            int pairB)
        {
            if (profile == null || !IsLogbookItemId(logbookItemId))
            {
                return false;
            }

            if (!profile.Ledger.Owns(logbookItemId))
            {
                return false;
            }

            if (!profile.BenchNotes.RevealPool(pairA, pairB))
            {
                return false;
            }

            // Consume the Logbook card (ever-owned can stay via a separate flag later;
            // greybox Revoke so it cannot be re-given).
            profile.Ledger.Revoke(logbookItemId);
            return true;
        }

        /// <summary>
        /// When pool is revealed, list affinity-pool item ids at target tier (and step-downs).
        /// Empty when not revealed.
        /// </summary>
        public static int[] RevealedPoolItemIds(
            PlayerProfile profile,
            ContentCatalog catalog,
            int pairA,
            int pairB,
            int targetTier)
        {
            if (profile == null || catalog == null
                || !profile.BenchNotes.IsPoolRevealed(pairA, pairB))
            {
                return System.Array.Empty<int>();
            }

            catalog.TryGetItem(pairA, out ItemRecord a);
            catalog.TryGetItem(pairB, out ItemRecord b);
            ItemTag affinity = TinkerRuntime.AffinitySet(a, b, ItemTag.None, out _);
            var ids = new System.Collections.Generic.List<int>();
            var weights = new System.Collections.Generic.List<int>();
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int tier = targetTier; tier >= 1; tier--)
            {
                TinkerRuntime.BuildPool(catalog, affinity, tier, ids, weights);
                for (int i = 0; i < ids.Count; i++)
                {
                    seen.Add(ids[i]);
                }
            }

            int[] arr = new int[seen.Count];
            seen.CopyTo(arr);
            System.Array.Sort(arr);
            return arr;
        }
    }
}
