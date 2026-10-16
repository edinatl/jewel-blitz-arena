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

        /// <summary>
        /// Pocket Items count (0–<see cref="MatchRules.MaxPocketItems"/>). Custom/Online only;
        /// Meta fills when House Rule Pocket Items is on (items-and-economy.md §2.3).
        /// </summary>
        public int PocketItemCount;

        /// <summary>Pocketed item ids joining the crate pool at ×3 rarity weight.</summary>
        public fixed int PocketItemIds[MatchRules.MaxPocketItems];

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
                PocketItemCount = 0,
            };

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                config.FighterIds[i] = i;
                config.StartingLife[i] = 0;
                config.Teams[i] = (byte)i;
                config.CpuLevels[i] = (byte)CpuBrainRuntime.DefaultLevel;
            }

            for (int i = 0; i < MatchRules.MaxPocketItems; i++)
            {
                config.PocketItemIds[i] = 0;
            }

            return config;
        }

        /// <summary>Copy up to <see cref="MatchRules.MaxPocketItems"/> pocket ids (zeros skipped).</summary>
        public void SetPocketItems(int[]? itemIds)
        {
            for (int i = 0; i < MatchRules.MaxPocketItems; i++)
            {
                PocketItemIds[i] = 0;
            }

            PocketItemCount = 0;
            if (itemIds == null)
            {
                return;
            }

            for (int i = 0; i < itemIds.Length && PocketItemCount < MatchRules.MaxPocketItems; i++)
            {
                int id = itemIds[i];
                if (id == 0 || ContainsPocketItem(id))
                {
                    continue;
                }

                PocketItemIds[PocketItemCount++] = id;
            }
        }

        public bool ContainsPocketItem(int itemId)
        {
            if (itemId == 0)
            {
                return false;
            }

            for (int i = 0; i < PocketItemCount && i < MatchRules.MaxPocketItems; i++)
            {
                if (PocketItemIds[i] == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetPocketItemId(int index)
        {
            if ((uint)index >= (uint)PocketItemCount || index >= MatchRules.MaxPocketItems)
            {
                return 0;
            }

            return PocketItemIds[index];
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
