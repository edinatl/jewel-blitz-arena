using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Single blittable match snapshot. Pack=1 so hashing bytes never sees padding.
    /// Pool caps match <c>docs/tech/architecture.md</c> §2.3.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MatchState
    {
        // --- RNG ---
        public Xoshiro256StarStar GameplayRng;
        public Xoshiro256StarStar CpuRng0;
        public Xoshiro256StarStar CpuRng1;
        public Xoshiro256StarStar CpuRng2;
        public Xoshiro256StarStar CpuRng3;

        // --- Actors (explicit slots; fixed buffers cannot hold custom structs) ---
        public ActorSlot Actor0;
        public ActorSlot Actor1;
        public ActorSlot Actor2;
        public ActorSlot Actor3;

        // --- Reserved pools (byte buffers; stride checked in tests) ---
        public fixed byte Jewels[PoolCaps.Jewels * JewelStride];
        public fixed byte LooseItems[PoolCaps.LooseItems * LooseItemStride];
        public fixed byte Projectiles[PoolCaps.Projectiles * ProjectileStride];
        public fixed byte BossSubActors[PoolCaps.BossSubActors * GenericStride];
        public fixed byte Summons[PoolCaps.Summons * GenericStride];
        public fixed byte KinematicBodies[PoolCaps.KinematicBodies * GenericStride];
        public fixed byte HazardVolumes[PoolCaps.HazardVolumes * GenericStride];
        public fixed byte Crates[PoolCaps.Crates * GenericStride];
        public fixed byte GleamPickups[PoolCaps.GleamPickups * GenericStride];
        public fixed byte StageCreatures[PoolCaps.StageCreatures * GenericStride];
        public fixed byte CpuBrains[PoolCaps.CpuBrains * CpuBrainStride];

        // --- Match header ---
        public ulong Seed;
        public int Frame;
        public int ActorCount;
        public int StageId;
        public int ActiveJewelCount;
        public int ActiveLooseItemCount;
        public int ActiveProjectileCount;
        public uint ReservedHeader;

        // Strides must equal sizeof(T) for the corresponding slot type (enforced by tests).
        public const int JewelStride = 52;
        public const int LooseItemStride = 32;
        public const int ProjectileStride = 56;
        public const int GenericStride = 36;
        public const int CpuBrainStride = 16;

        public static int SizeOf => sizeof(MatchState);
    }

    /// <summary>Helpers that take <c>ref MatchState</c> so accessors do not return refs from a struct copy.</summary>
    public static class MatchStateAccess
    {
        public static ref ActorSlot Actor(ref MatchState state, int index)
        {
            switch (index)
            {
                case 0: return ref state.Actor0;
                case 1: return ref state.Actor1;
                case 2: return ref state.Actor2;
                case 3: return ref state.Actor3;
                default: return ref state.Actor0;
            }
        }

        public static ref Xoshiro256StarStar CpuRng(ref MatchState state, int index)
        {
            switch (index)
            {
                case 0: return ref state.CpuRng0;
                case 1: return ref state.CpuRng1;
                case 2: return ref state.CpuRng2;
                case 3: return ref state.CpuRng3;
                default: return ref state.CpuRng0;
            }
        }
    }
}
