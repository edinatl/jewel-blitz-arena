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

        public fixed int FighterIds[PoolCaps.Actors];
        public fixed int StartingLife[PoolCaps.Actors];
        public fixed byte Teams[PoolCaps.Actors];

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
            };

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                config.FighterIds[i] = i;
                config.StartingLife[i] = 0;
                config.Teams[i] = (byte)i;
            }

            return config;
        }
    }
}
