namespace JewelBlitzArena.Sim
{
    /// <summary>Fixed pool caps from <c>docs/tech/architecture.md</c> §2.3.</summary>
    public static class PoolCaps
    {
        public const int Actors = 4;
        public const int BossSubActors = 8;
        public const int LooseItems = 12;
        public const int Projectiles = 32;
        public const int Summons = 2;
        public const int KinematicBodies = 16;
        public const int HazardVolumes = 32;
        public const int Jewels = 10;
        public const int Crates = 8;
        public const int GleamPickups = 24;
        public const int StageCreatures = 8;
        public const int CpuBrains = 4;
    }
}
