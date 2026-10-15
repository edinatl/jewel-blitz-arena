using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ActorSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public Fix VelX;
        public Fix VelY;
        public Fix VelZ;
        public int Life;
        public int Luster;
        public int FighterId;
        public int DrossFrames; // S2: jewel collect lockout after Blitz revert (S1 sets)
        public byte JewelsHeld;
        public byte Team;
        public byte Flags;
        public byte HeldItemSlot; // 255 = empty hands

        public const byte FlagAlive = 1 << 0;
        public const byte FlagBlitzed = 1 << 1;
        public const byte NoHeldItem = 255;

        public static int SizeOf
        {
            get
            {
                unsafe
                {
                    return sizeof(ActorSlot);
                }
            }
        }

        public bool IsAlive
        {
            get => (Flags & FlagAlive) != 0;
            set
            {
                if (value)
                {
                    Flags = (byte)(Flags | FlagAlive);
                }
                else
                {
                    Flags = (byte)(Flags & ~FlagAlive);
                }
            }
        }

        public bool IsBlitzed
        {
            get => (Flags & FlagBlitzed) != 0;
            set
            {
                if (value)
                {
                    Flags = (byte)(Flags | FlagBlitzed);
                }
                else
                {
                    Flags = (byte)(Flags & ~FlagBlitzed);
                }
            }
        }

        public bool HasEmptyHands => HeldItemSlot == NoHeldItem;
    }
}
