using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Outcome handed back to mode orchestrators after a match ends.
    /// S0 fills placements/KOs stubs; economy fields stay zero until S6.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct MatchResult
    {
        public int FramesPlayed;
        public int ActorCount;

        /// <summary>1 = first place …; 0 = unused slot.</summary>
        public fixed int Placements[PoolCaps.Actors];

        public fixed int Kos[PoolCaps.Actors];

        public fixed int JewelsCollected[PoolCaps.Actors];

        public int CaratsEarned;
    }
}
