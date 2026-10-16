namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Pocket Items — up to five pocketable owned item ids (items-and-economy.md §2.3).
    /// Greybox: flags only; crate weight ×3 applies when House Rule Pocket Items is on.
    /// </summary>
    public sealed class PocketLoadout
    {
        public const int MaxSlots = 5;

        private readonly int[] _slots = new int[MaxSlots];

        public int Count
        {
            get
            {
                int n = 0;
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (_slots[i] != 0)
                    {
                        n++;
                    }
                }

                return n;
            }
        }

        public bool Contains(int itemId)
        {
            if (itemId == 0)
            {
                return false;
            }

            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryAdd(int itemId)
        {
            if (itemId == 0 || Contains(itemId))
            {
                return false;
            }

            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == 0)
                {
                    _slots[i] = itemId;
                    return true;
                }
            }

            return false;
        }

        public bool TryRemove(int itemId)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] == itemId)
                {
                    _slots[i] = 0;
                    Compact();
                    return true;
                }
            }

            return false;
        }

        public int[] ToArray()
        {
            int n = Count;
            var arr = new int[n];
            int w = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != 0)
                {
                    arr[w++] = _slots[i];
                }
            }

            return arr;
        }

        public void ReplaceAll(int[]? ids)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                _slots[i] = 0;
            }

            if (ids == null)
            {
                return;
            }

            int w = 0;
            for (int i = 0; i < ids.Length && w < MaxSlots; i++)
            {
                if (ids[i] != 0 && !Contains(ids[i]))
                {
                    _slots[w++] = ids[i];
                }
            }
        }

        private void Compact()
        {
            int w = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (_slots[i] != 0)
                {
                    _slots[w++] = _slots[i];
                }
            }

            for (int i = w; i < MaxSlots; i++)
            {
                _slots[i] = 0;
            }
        }
    }
}
