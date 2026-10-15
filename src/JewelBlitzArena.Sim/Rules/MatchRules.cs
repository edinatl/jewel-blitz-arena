namespace JewelBlitzArena.Sim
{
    /// <summary>Constants from <c>combat-and-match.md</c> / <c>balance.md</c> used by S1.</summary>
    public static class MatchRules
    {
        public const int LusterMax = 1200;
        public const int LusterDrainPerFrame = 1;
        public const int SurgeLusterCost = 420;
        public const int BlitzTransformFrames = 30;
        public const int RevertFrames = 20;
        public const int DrossFrames = 6 * 60;
        public const long JewelPickupRangeRaw = Fix.OneRaw; // 1.0 m
        public const long HitRangeBaseRaw = Fix.OneRaw + (Fix.OneRaw / 5); // 1.2 m
        public const long HitRangePerReachRaw = Fix.OneRaw / 5; // +0.2 m per Reach rating
        public const int KnockdownMinFrames = 30;
        public const int KnockdownMaxFrames = 60;
        public const int HitStunBase = 16;
        public const int DodgeArmFrames = 6;
        public const int DodgeInvulnFrames = 14;
        public const int DodgeCooldownFrames = 40;
        public const int BodyPropFrames = 60;
        public const int OpeningJewelDelayFrames = 3 * 60;
        public const int JewelRespawnDelayFrames = 4 * 60;
        public const int AttackStringWindowFrames = 20;
        public const int ComboResetFrames = 30;
        public const int ComboForceKnockdownAt = 6;
        public const int BlitzedDamageTakenPercent = 50;
        public const int BlitzJewelCount = 3;

        public const byte HouseLusterTimer = 1 << 0;

        public static int DefaultJewelCap(int actorCount) =>
            actorCount <= 2 ? 3 : 5;

        public static long HitRangeRaw(int reachRating)
        {
            int r = reachRating < 1 ? 3 : (reachRating > 5 ? 5 : reachRating);
            return HitRangeBaseRaw + (HitRangePerReachRaw * r);
        }
    }
}
