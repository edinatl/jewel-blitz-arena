using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Owned-item Ledger. Ownership gates crate *eligibility* (modes/S5), not spawn rates.
    /// </summary>
    public sealed class LedgerInventory
    {
        private readonly HashSet<int> _ownedItemIds = new HashSet<int>();

        /// <summary>S6 greybox token — written once after Custom greybox results.</summary>
        public const int CustomGreyboxTokenItemId = 900001;

        public int Count => _ownedItemIds.Count;

        public bool Owns(int itemId) => _ownedItemIds.Contains(itemId);

        public bool Grant(int itemId) => _ownedItemIds.Add(itemId);

        public bool Revoke(int itemId) => _ownedItemIds.Remove(itemId);

        public IReadOnlyCollection<int> OwnedItemIds => _ownedItemIds;

        public void Clear() => _ownedItemIds.Clear();

        public void ReplaceAll(IEnumerable<int> itemIds)
        {
            _ownedItemIds.Clear();
            if (itemIds == null)
            {
                return;
            }

            foreach (int id in itemIds)
            {
                _ownedItemIds.Add(id);
            }
        }

        public int[] ToArray()
        {
            var arr = new int[_ownedItemIds.Count];
            _ownedItemIds.CopyTo(arr);
            Array.Sort(arr);
            return arr;
        }
    }
}
