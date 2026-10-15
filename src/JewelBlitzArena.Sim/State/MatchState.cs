using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Single blittable match snapshot. Pack=1 so hashing bytes never sees padding.
    /// Pool caps match <c>docs/tech/architecture.md</c> §2.3.
    /// Integrated S1 (placements/respawn schedule) + S2 (loose-item stride 56).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MatchState
    {
        public Xoshiro256StarStar GameplayRng;
        public Xoshiro256StarStar CpuRng0;
        public Xoshiro256StarStar CpuRng1;
        public Xoshiro256StarStar CpuRng2;
        public Xoshiro256StarStar CpuRng3;

        public ActorSlot Actor0;
        public ActorSlot Actor1;
        public ActorSlot Actor2;
        public ActorSlot Actor3;

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

        public ulong Seed;
        public int Frame;
        public int ActorCount;
        public int StageId;
        public int ActiveJewelCount;
        public int ActiveLooseItemCount;
        public int ActiveProjectileCount;
        public int JewelCap;
        public int NextJewelSpawnFrame;
        public int HouseRulesBits;
        public int MatchEndFrame;
        public int NextPlacementFromBottom;
        public fixed int PlacementBySlot[PoolCaps.Actors];
        public fixed int JewelRespawnFrame[PoolCaps.Jewels];

        // S4 stage graph runtime (hashed)
        public int PhaseIndex;
        public int PhaseElapsedFrames;
        public int PhaseDurationFrames;
        public int TransitionRemainingFrames;
        public int ActiveGeometrySetId;
        public int StageFlags;
        public int PhaseClockPermille;
        public int ActiveHazardCount;
        public int ActiveKinematicCount;

        public const int JewelStride = 52;
        public const int LooseItemStride = 56; // S2: includes velocities
        public const int ProjectileStride = 56;
        public const int GenericStride = 36;
        public const int CpuBrainStride = 16;

        public static int SizeOf => sizeof(MatchState);

        public bool MatchOver => MatchEndFrame > 0;
    }

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

        public static JewelSlot GetJewel(ref MatchState state, int index)
        {
            unsafe
            {
                fixed (byte* p = state.Jewels)
                {
                    return ((JewelSlot*)p)[index];
                }
            }
        }

        public static void SetJewel(ref MatchState state, int index, in JewelSlot jewel)
        {
            unsafe
            {
                fixed (byte* p = state.Jewels)
                {
                    ((JewelSlot*)p)[index] = jewel;
                }
            }
        }

        public static LooseItemSlot GetLooseItem(ref MatchState state, int index)
        {
            unsafe
            {
                fixed (byte* p = state.LooseItems)
                {
                    return ((LooseItemSlot*)p)[index];
                }
            }
        }

        public static void SetLooseItem(ref MatchState state, int index, in LooseItemSlot item)
        {
            unsafe
            {
                fixed (byte* p = state.LooseItems)
                {
                    ((LooseItemSlot*)p)[index] = item;
                }
            }
        }

        public static int GetJewelRespawnFrame(ref MatchState state, int index)
        {
            unsafe
            {
                return state.JewelRespawnFrame[index];
            }
        }

        public static void SetJewelRespawnFrame(ref MatchState state, int index, int frame)
        {
            unsafe
            {
                state.JewelRespawnFrame[index] = frame;
            }
        }

        public static void SetPlacement(ref MatchState state, int slot, int place)
        {
            unsafe
            {
                state.PlacementBySlot[slot] = place;
            }
        }

        public static int GetPlacement(ref MatchState state, int slot)
        {
            unsafe
            {
                return state.PlacementBySlot[slot];
            }
        }

        public static GenericPoolSlot GetHazard(ref MatchState state, int index)
        {
            unsafe
            {
                fixed (byte* p = state.HazardVolumes)
                {
                    return ((GenericPoolSlot*)p)[index];
                }
            }
        }

        public static void SetHazard(ref MatchState state, int index, in GenericPoolSlot slot)
        {
            unsafe
            {
                fixed (byte* p = state.HazardVolumes)
                {
                    ((GenericPoolSlot*)p)[index] = slot;
                }
            }
        }

        public static GenericPoolSlot GetKinematic(ref MatchState state, int index)
        {
            unsafe
            {
                fixed (byte* p = state.KinematicBodies)
                {
                    return ((GenericPoolSlot*)p)[index];
                }
            }
        }

        public static void SetKinematic(ref MatchState state, int index, in GenericPoolSlot slot)
        {
            unsafe
            {
                fixed (byte* p = state.KinematicBodies)
                {
                    ((GenericPoolSlot*)p)[index] = slot;
                }
            }
        }
    }
}
