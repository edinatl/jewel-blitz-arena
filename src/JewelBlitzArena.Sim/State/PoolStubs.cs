using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct JewelSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public Fix VelX;
        public Fix VelY;
        public Fix VelZ;
        public byte OwnerSlot; // 255 = free on field / inactive
        public byte Flags;
        public ushort PickupDelayFrames;

        public const byte FlagActive = 1 << 0;
        public const byte NoOwner = 255;

        public bool IsActive
        {
            get => (Flags & FlagActive) != 0;
            set
            {
                if (value)
                {
                    Flags = (byte)(Flags | FlagActive);
                }
                else
                {
                    Flags = (byte)(Flags & ~FlagActive);
                }
            }
        }

        public bool IsOnField => IsActive && OwnerSlot == NoOwner;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LooseItemSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public Fix VelX;
        public Fix VelY;
        public Fix VelZ;
        public int ItemId;
        public byte HolderSlot; // 255 = on field / inactive
        public byte Flags;
        public ushort FlightFrames;

        public const byte FlagActive = 1 << 0;
        public const byte FlagInFlight = 1 << 1;
        public const byte NoHolder = 255;

        public bool IsActive
        {
            get => (Flags & FlagActive) != 0;
            set
            {
                if (value)
                {
                    Flags = (byte)(Flags | FlagActive);
                }
                else
                {
                    Flags = (byte)(Flags & ~FlagActive);
                }
            }
        }

        public bool InFlight
        {
            get => (Flags & FlagInFlight) != 0;
            set
            {
                if (value)
                {
                    Flags = (byte)(Flags | FlagInFlight);
                }
                else
                {
                    Flags = (byte)(Flags & ~FlagInFlight);
                }
            }
        }

        public bool IsOnField => IsActive && HolderSlot == NoHolder && !InFlight;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProjectileSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public Fix VelX;
        public Fix VelY;
        public Fix VelZ;
        public int Damage;
        public byte OwnerSlot;
        public byte Flags;
        public ushort Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GenericPoolSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public int TypeId;
        public int Param0;
        public byte Flags;
        public byte Reserved0;
        public ushort Reserved1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CpuBrainSlot
    {
        public int ThinkCooldown;
        public int TargetSlot;

        /// <summary>Packed <see cref="InputFrame.Bits"/> from the last think.</summary>
        public int LastDecision;

        /// <summary>Bit 0 = active CPU control (<see cref="CpuBrainRuntime.FlagActive"/>).</summary>
        public uint Reserved;
    }
}
