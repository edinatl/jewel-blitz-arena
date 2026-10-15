namespace JewelBlitzArena.Sim
{
    /// <summary>Constants from <c>combat-and-match.md</c> / <c>balance.md</c> used by S1.</summary>
    public static class MatchRules
    {
        public const int LusterMax = 1200;
        public const int LusterDrainPerFrame = 1;
        public const int SurgeLusterCost = 420;

        /// <summary>Tilly Invoice (Surge A) only — balance.md §3.3 / cast.md §5.</summary>
        public const int InvoiceSurgeLusterCost = 180;

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

        /// <summary>
        /// Luster cost for a Surge press. InvoiceRepeatable kits pay
        /// <see cref="InvoiceSurgeLusterCost"/> on Surge A only; Surge B and all other kits use
        /// <see cref="SurgeLusterCost"/>.
        /// </summary>
        public static int SurgeLusterCostFor(KitBehavior behavior, bool surgeA)
        {
            if (surgeA && behavior == KitBehavior.InvoiceRepeatable)
            {
                return InvoiceSurgeLusterCost;
            }

            return SurgeLusterCost;
        }

        /// <summary>Magnet Surge pull / strip radius (cast.md Clank Spare Parts).</summary>
        public static readonly Fix MagnetSurgeRange = Fix.FromInt(6);

        /// <summary>
        /// Clank Toolbox / Spare Parts direction index: 0 neutral, 1 forward, 2 up, 3 down
        /// (names.md / cast.md §2). Magnitude 0 → neutral; stick near +Z → up, −Z → down;
        /// otherwise forward. Forward is facing-relative (<paramref name="facing"/> 0 = +X, 1 = −X).
        /// </summary>
        public static int SurgeVariantIndex(in InputFrame input, byte facing = 0)
        {
            if (input.Magnitude == 0)
            {
                return 0;
            }

            int d = input.Direction & 31;
            int forwardDir = facing == 0 ? 0 : 16;
            int distUp = DirDistance(d, 8);
            int distDown = DirDistance(d, 24);
            int distFwd = DirDistance(d, forwardDir);

            // Prefer vertical when clearly closer to up/down than to facing-forward.
            if (distUp <= 4 && distUp < distFwd && distUp <= distDown)
            {
                return 2;
            }

            if (distDown <= 4 && distDown < distFwd)
            {
                return 3;
            }

            return 1; // facing-relative forward (back stick also maps here — no back variant)
        }

        private static int DirDistance(int a, int b)
        {
            int diff = a - b;
            if (diff < 0)
            {
                diff = -diff;
            }

            if (diff > 16)
            {
                diff = 32 - diff;
            }

            return diff;
        }
    }
}
