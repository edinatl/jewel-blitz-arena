using JewelBlitzArena.Data;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Fitting Room equipped cosmetics — one item id per <see cref="CosmeticSocket"/>
    /// (cast.md §7 / items-and-economy.md §1). 0 = empty. Cosmetic only; no Sim hitbox change.
    /// </summary>
    public sealed class FittingLoadout
    {
        public const int SlotCount = 5;

        private readonly int[] _slots = new int[SlotCount];

        public int Get(CosmeticSocket socket)
        {
            int i = SlotIndex(socket);
            return i < 0 ? 0 : _slots[i];
        }

        public void Set(CosmeticSocket socket, int itemId)
        {
            int i = SlotIndex(socket);
            if (i < 0)
            {
                return;
            }

            _slots[i] = itemId;
        }

        public void Clear(CosmeticSocket socket) => Set(socket, 0);

        public void ClearAll()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                _slots[i] = 0;
            }
        }

        public int[] ToArray()
        {
            var copy = new int[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                copy[i] = _slots[i];
            }

            return copy;
        }

        public void ReplaceAll(int[]? slots)
        {
            ClearAll();
            if (slots == null)
            {
                return;
            }

            int n = slots.Length < SlotCount ? slots.Length : SlotCount;
            for (int i = 0; i < n; i++)
            {
                _slots[i] = slots[i];
            }
        }

        /// <summary>Head, Arm, Waist, Leg, Back → 0..4. None → -1.</summary>
        public static int SlotIndex(CosmeticSocket socket)
        {
            switch (socket)
            {
                case CosmeticSocket.Head:
                    return 0;
                case CosmeticSocket.Arm:
                    return 1;
                case CosmeticSocket.Waist:
                    return 2;
                case CosmeticSocket.Leg:
                    return 3;
                case CosmeticSocket.Back:
                    return 4;
                default:
                    return -1;
            }
        }

        public static CosmeticSocket SocketAt(int index)
        {
            switch (index)
            {
                case 0:
                    return CosmeticSocket.Head;
                case 1:
                    return CosmeticSocket.Arm;
                case 2:
                    return CosmeticSocket.Waist;
                case 3:
                    return CosmeticSocket.Leg;
                case 4:
                    return CosmeticSocket.Back;
                default:
                    return CosmeticSocket.None;
            }
        }
    }
}
