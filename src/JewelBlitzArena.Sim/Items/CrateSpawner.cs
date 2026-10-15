using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Crate roll API. Category/item weights from design; ownership gating is Meta/S6.
    /// S2 only needs a deterministic pick from an eligible id list (S5 catalog).
    /// </summary>
    public static class CrateSpawner
    {
        /// <summary>
        /// Two-stage roll stub: pick uniformly among always-eligible items when no table is built.
        /// Returns item_id or -1 if empty.
        /// </summary>
        public static int RollAlwaysEligible(ref MatchState state, ContentCatalog? catalog)
        {
            if (catalog == null)
            {
                return -1;
            }

            var always = catalog.AlwaysEligibleItems();
            if (always.Count == 0)
            {
                return -1;
            }

            int index = state.GameplayRng.NextInt(always.Count);
            return always[index].ItemId;
        }

        /// <summary>Spawn a rolled item at a world position (stage prop / crate break).</summary>
        public static int TrySpawnLooseItem(ref MatchState state, int itemId, FixVec3 pos)
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
