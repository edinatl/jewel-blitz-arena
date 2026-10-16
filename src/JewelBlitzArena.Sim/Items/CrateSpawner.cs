using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Crate roll API. Category/item weights from design; ownership gating is Meta/S6.
    /// S2 only needs a deterministic pick from an eligible id list (S5 catalog).
    /// Pocket Items (MatchConfig) join at ×3 rarity weight when House Rule is on
    /// (items-and-economy.md §2.3). Empty pockets keep the uniform AlwaysEligible path
    /// so existing corpora stay hash-stable.
    /// </summary>
    public static class CrateSpawner
    {
        /// <summary>base_weight by rarity 1..7 (items-and-economy.md §2).</summary>
        public static int BaseWeightForRarity(int rarity)
        {
            switch (rarity)
            {
                case 1: return 100;
                case 2: return 70;
                case 3: return 45;
                case 4: return 25;
                case 5: return 12;
                case 6: return 5;
                case 7: return 2;
                default: return rarity < 1 ? 100 : 2;
            }
        }

        /// <summary>
        /// Two-stage roll stub: uniform among always-eligible when no pockets;
        /// rarity-weighted Always ∪ pocket (×3) when pockets are present.
        /// Returns item_id or -1 if empty.
        /// </summary>
        public static int RollAlwaysEligible(
            ref MatchState state,
            ContentCatalog? catalog,
            int[]? pocketItemIds = null,
            int pocketItemCount = 0)
        {
            if (catalog == null)
            {
                return -1;
            }

            var always = catalog.AlwaysEligibleItems();
            int pocketCount = ClampPocketCount(pocketItemIds, pocketItemCount);
            if (pocketCount <= 0)
            {
                if (always.Count == 0)
                {
                    return -1;
                }

                int index = state.GameplayRng.NextInt(always.Count);
                return always[index].ItemId;
            }

            return RollWeightedWithPockets(ref state, catalog, always, pocketItemIds!, pocketCount);
        }

        private static int ClampPocketCount(int[]? pocketItemIds, int pocketItemCount)
        {
            if (pocketItemIds == null || pocketItemCount <= 0)
            {
                return 0;
            }

            int n = pocketItemCount;
            if (n > pocketItemIds.Length)
            {
                n = pocketItemIds.Length;
            }

            if (n > MatchRules.MaxPocketItems)
            {
                n = MatchRules.MaxPocketItems;
            }

            return n;
        }

        private static bool IsPocketed(int itemId, int[] pocketItemIds, int pocketCount)
        {
            if (itemId == 0)
            {
                return false;
            }

            for (int i = 0; i < pocketCount; i++)
            {
                if (pocketItemIds[i] == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int RollWeightedWithPockets(
            ref MatchState state,
            ContentCatalog catalog,
            System.Collections.Generic.IReadOnlyList<ItemRecord> always,
            int[] pocketItemIds,
            int pocketCount)
        {
            // Stack buffers: always (≤ ~40 starters) + up to 5 pocket-only ids.
            const int MaxPool = 64;
            int[] ids = new int[MaxPool];
            int[] weights = new int[MaxPool];
            int n = 0;
            int total = 0;

            for (int i = 0; i < always.Count && n < MaxPool; i++)
            {
                ItemRecord item = always[i];
                int w = BaseWeightForRarity(item.Rarity);
                if (IsPocketed(item.ItemId, pocketItemIds, pocketCount))
                {
                    w *= 3;
                }

                if (w <= 0)
                {
                    continue;
                }

                ids[n] = item.ItemId;
                weights[n] = w;
                total += w;
                n++;
            }

            for (int p = 0; p < pocketCount && n < MaxPool; p++)
            {
                int pocketId = pocketItemIds[p];
                if (pocketId == 0)
                {
                    continue;
                }

                bool already = false;
                for (int i = 0; i < n; i++)
                {
                    if (ids[i] == pocketId)
                    {
                        already = true;
                        break;
                    }
                }

                if (already)
                {
                    continue;
                }

                if (!catalog.TryGetItem(pocketId, out ItemRecord item))
                {
                    continue;
                }

                int w = BaseWeightForRarity(item.Rarity) * 3;
                if (w <= 0)
                {
                    continue;
                }

                ids[n] = pocketId;
                weights[n] = w;
                total += w;
                n++;
            }

            if (n == 0 || total <= 0)
            {
                return -1;
            }

            int pick = state.GameplayRng.NextInt(total);
            for (int i = 0; i < n; i++)
            {
                pick -= weights[i];
                if (pick < 0)
                {
                    return ids[i];
                }
            }

            return ids[n - 1];
        }

        /// <summary>Spawn a rolled item at a world position (stage prop / crate break).</summary>
        public static int TrySpawnLooseItem(ref MatchState state, int itemId, FixVec3 pos) =>
            TrySpawnLooseItem(ref state, itemId, pos, stageOnly: false);

        /// <summary>
        /// Spawn a loose item; <paramref name="stageOnly"/> marks Vein Guard / arena weapons
        /// that must not enter Meta inventory.
        /// </summary>
        public static int TrySpawnLooseItem(
            ref MatchState state, int itemId, FixVec3 pos, bool stageOnly)
        {
            if (itemId < 0)
            {
                return -1;
            }

            int slot = FindInactiveItemSlot(ref state);
            if (slot < 0)
            {
                return -1;
            }

            var item = new LooseItemSlot
            {
                PosX = pos.X,
                PosY = pos.Y + PhysicsConstants.ItemRadius,
                PosZ = pos.Z,
                ItemId = itemId,
                HolderSlot = LooseItemSlot.NoHolder,
            };
            item.IsActive = true;
            item.IsStageOnly = stageOnly;
            MatchStateAccess.SetLooseItem(ref state, slot, item);
            state.ActiveLooseItemCount = CountActiveItems(ref state);
            return slot;
        }

        public static int CountActiveItems(ref MatchState state)
        {
            int n = 0;
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                if (MatchStateAccess.GetLooseItem(ref state, i).IsActive)
                {
                    n++;
                }
            }

            return n;
        }

        private static int FindInactiveItemSlot(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                if (!MatchStateAccess.GetLooseItem(ref state, i).IsActive)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
