namespace JewelBlitzArena.Sim
{
    public static class StageFlags
    {
        public const int FallIsKo = 1 << 0;
        public const int BlitzSuppressed = 1 << 1;
        public const int InTransition = 1 << 2;
        public const int HazardsEnabled = 1 << 3;
    }
}
