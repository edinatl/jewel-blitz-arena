using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>Placeholder blittable slots so <see cref="MatchState"/> reserves real pool caps.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct JewelSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public Fix VelX;
        public Fix VelY;
        public Fix VelZ;
        public byte OwnerSlot; // 255 = free on field
        public byte Flags;
        public ushort Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LooseItemSlot
    {
        public Fix PosX;
        public Fix PosY;
        public Fix PosZ;
        public int ItemId;
        public byte HolderSlot; // 255 = on field
        public byte Flags;
        public ushort Reserved;
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
        public int LastDecision;
        public uint Reserved;
    }
}
