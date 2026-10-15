using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Quantized per-actor input (2 bytes). Layout matches architecture §3:
    /// stick angle 5 bits (32 dirs), magnitude 2 bits (4 bands), six logical buttons.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InputFrame
    {
        public const int SizeOf = 2;

        /// <summary>
        /// Bit packing:
        /// 0..4 direction, 5..6 magnitude, 8 Jump, 9 Attack, 10 Action, 11 Strong, 12 SurgeA, 13 SurgeB.
        /// Bits 7 and 14..15 reserved (must be zero for stable hashes of logged inputs).
        /// </summary>
        public ushort Bits;

        public static InputFrame Neutral => default;

        public int Direction
        {
            get => Bits & 0x1F;
            set => Bits = (ushort)((Bits & ~0x1F) | (value & 0x1F));
        }

        public int Magnitude
        {
            get => (Bits >> 5) & 0x3;
            set => Bits = (ushort)((Bits & ~(0x3 << 5)) | ((value & 0x3) << 5));
        }

        public bool Jump
        {
            get => (Bits & (1 << 8)) != 0;
            set => SetButton(8, value);
        }

        public bool Attack
        {
            get => (Bits & (1 << 9)) != 0;
            set => SetButton(9, value);
        }

        public bool Action
        {
            get => (Bits & (1 << 10)) != 0;
            set => SetButton(10, value);
        }

        public bool Strong
        {
            get => (Bits & (1 << 11)) != 0;
            set => SetButton(11, value);
        }

        public bool SurgeA
        {
            get => (Bits & (1 << 12)) != 0;
            set => SetButton(12, value);
        }

        public bool SurgeB
        {
            get => (Bits & (1 << 13)) != 0;
            set => SetButton(13, value);
        }

        public static InputFrame FromStick(int direction, int magnitude)
        {
            var frame = Neutral;
            frame.Direction = direction;
            frame.Magnitude = magnitude;
            return frame;
        }

        private void SetButton(int bit, bool value)
        {
            if (value)
            {
                Bits = (ushort)(Bits | (1 << bit));
            }
            else
            {
                Bits = (ushort)(Bits & ~(1 << bit));
            }
        }
    }
}
