using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Immutable-at-step match setup. Modes build this; Sim never reads a profile.
    /// Replays are <see cref="MatchConfig"/> + an input log.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MatchConfig
    {
        public ulong Seed;
        public int StageId;
        public int ActorCount;

        /// <summary>0 = auto from actor count (2→3, 3–4→5). House Rule 2 may set 3–7.</summary>
        public int JewelCap;

        /// <summary><see cref="MatchRules.HouseLusterTimer"/> and future house-rule bits.</summary>
        public int HouseRulesBits;

        /// <summary>0 = free-for-all (Rally off). Non-zero reserved for teams (S6+).</summary>
        public int TeamMode;

        /// <summary>House Rule 7 Phase Clock as permille (1000 = 100%). Scales every phase timer.</summary>
        public int PhaseClockPermille;

        /// <summary>Expedition gleam pickup scale (1000 = 1.0). Default 1000 for non-Expedition.</summary>
        public int GleamPickupScalePermille;

        /// <summary>
        /// Boss part / Vitre Life scale (1000 = 1.0). Meta premultiplies difficulty × participant.
        /// </summary>
        public int BossLifeScalePermille;

        public fixed int FighterIds[PoolCaps.Actors];
        public fixed int StartingLife[PoolCaps.Actors];
        public fixed byte Teams[PoolCaps.Actors];

        /// <summary>Per-slot CPU level 1–8 (combat §10.1). Default 4. Used when a slot is CPU-filled.</summary>
        public fixed byte CpuLevels[PoolCaps.Actors];

        public static MatchConfig CreateDefault(ulong seed, int actorCount = 2)
        {
            var config = new MatchConfig
            {
                Seed = seed,
                StageId = 0,
                ActorCount = actorCount < 1 ? 1 : (actorCount > PoolCaps.Actors ? PoolCaps.Actors : actorCount),
                JewelCap = 0,
                HouseRulesBits = MatchRules.HouseLusterTimer,
                TeamMode = 0,
                PhaseClockPermille = 1000,
                GleamPickupScalePermille = 1000,
                BossLifeScalePermille = 1000,
            };

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                config.FighterIds[i] = i;
                config.StartingLife[i] = 0;
                config.Teams[i] = (byte)i;
                config.CpuLevels[i] = (byte)CpuBrainRuntime.DefaultLevel;
            }

            return config;
        }

        public void SetFighterId(int slot, int fighterId)
        {
            if ((uint)slot >= PoolCaps.Actors)
            {
                throw new System.ArgumentOutOfRangeException(nameof(slot));
            }

            FighterIds[slot] = fighterId;
        }

        public void SetStartingLife(int slot, int life)
        {
            if ((uint)slot >= PoolCaps.Actors)
            {
                throw new System.ArgumentOutOfRangeException(nameof(slot));
            }

            StartingLife[slot] = life;
        }

        public void SetCpuLevel(int slot, int level)
        {
            if ((uint)slot >= PoolCaps.Actors)
            {
                throw new System.ArgumentOutOfRangeException(nameof(slot));
            }

            CpuLevels[slot] = (byte)CpuBrainRuntime.ClampLevel(level);
        }

        public int GetCpuLevel(int slot)
        {
            if ((uint)slot >= PoolCaps.Actors)
            {
                return CpuBrainRuntime.DefaultLevel;
            }

            int level = CpuLevels[slot];
            return level == 0 ? CpuBrainRuntime.DefaultLevel : CpuBrainRuntime.ClampLevel(level);
        }
    }
}
