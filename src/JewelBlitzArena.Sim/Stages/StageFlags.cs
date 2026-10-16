namespace JewelBlitzArena.Sim
{
    public static class StageFlags
    {
        public const int FallIsKo = 1 << 0;
        public const int BlitzSuppressed = 1 << 1;
        public const int InTransition = 1 << 2;
        public const int HazardsEnabled = 1 << 3;
        /// <summary>Boss timeout move already fired this match.</summary>
        public const int BossTimeoutFired = 1 << 4;
    }
}
