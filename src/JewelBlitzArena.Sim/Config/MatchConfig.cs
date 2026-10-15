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

        /// <summary>Fighter registry ids for slots 0..3 (unused slots ignored).</summary>
        public fixed int FighterIds[PoolCaps.Actors];

        /// <summary>Starting Life per slot; 0 means default (1000).</summary>
        public fixed int StartingLife[PoolCaps.Actors];

        public static MatchConfig CreateDefault(ulong seed, int actorCount = 2)
        {
            var config = new MatchConfig
            {
                Seed = seed,
                StageId = 0,
                ActorCount = actorCount < 1 ? 1 : (actorCount > PoolCaps.Actors ? PoolCaps.Actors : actorCount),
            };

            for (int i = 0; i < PoolCaps.Actors; i++)
            {
                config.FighterIds[i] = i;
                config.StartingLife[i] = 0;
            }

            return config;
        }
    }
}
