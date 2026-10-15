namespace JewelBlitzArena.Sim
{
    /// <summary>S2 greybox physics constants (Q48.16). Tunable later via Data.</summary>
    public static class PhysicsConstants
    {
        public static readonly Fix Gravity = new Fix(-(Fix.OneRaw * 20)); // −20 m/s²
        public static readonly Fix ActorRadius = new Fix(Fix.OneRaw / 2); // 0.5 m
        public static readonly Fix ActorHalfHeight = Fix.FromInt(1); // capsule half-height
        public static readonly Fix JewelRadius = new Fix(Fix.OneRaw / 4); // 0.25 m
        public static readonly Fix ItemRadius = new Fix(Fix.OneRaw / 3);
        public static readonly Fix PickupRange = Fix.FromInt(1); // 1 m ring
        public static readonly Fix ThrowSpeed = Fix.FromInt(12);
        public static readonly Fix BounceRestitution = new Fix(Fix.OneRaw / 2); // 0.5
        public static readonly Fix GroundFriction = new Fix((Fix.OneRaw * 85) / 100); // keep 85% horiz vel on ground
        public static readonly Fix ArenaHalfExtent = Fix.FromInt(8);
        public const int SolverIterations = 4;
        public const int CatchWindowFrames = 6;
        public const int JewelPickupDelayFrames = 36; // 0.6 s after spawn/drop
        public const int JewelOpenSpawnFrame = 180; // 3 s
        public const int JewelFollowupSpawnFrames = 300; // 5 s
    }
}
