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

        /// <summary>Expedition gleam drops on KO / containers (items-and-economy.md §7).</summary>
        /// <remarks>Bit 8 — avoids Meta HouseRulesBits PocketItems (bit 1) collision.</remarks>
        public const int ExpeditionGleams = 1 << 8;

        /// <summary>Copper gleam despawn (8 s).</summary>
        public const int GleamDespawnFrames = 8 * 60;

        public const int GleamCopperCarats = 100;
        public const int GleamSilverCarats = 500;
        public const int GleamGoldCarats = 1000;

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

        /// <summary>Teo Bateria area Stun (cast.md §5 / balance.md §4 max 60 f).</summary>
        public static readonly Fix BateriaStunRange = Fix.FromInt(8);
        public const int BateriaStunFrames = 60;

        /// <summary>Clank Spare Chaff Blind duration (balance.md §4 max 120 f).</summary>
        public static readonly Fix ChaffBlindRange = Fix.FromInt(6);
        public const int BlindDurationFrames = 120;

        /// <summary>Sable Smoke Vault area Dizzy (balance.md §4 max 90 f).</summary>
        public static readonly Fix SmokeVaultDizzyRange = Fix.FromInt(8);
        public const int DizzyDurationFrames = 90;
        public const int DizzyImmuneFrames = 300;
        /// <summary>Floor after mash shortening (~70% of max).</summary>
        public const int DizzyMashFloorFrames = 63;

        /// <summary>Clank Toolbox Mine Field Slow (balance.md §4 max 240 f).</summary>
        public static readonly Fix MineFieldSlowRange = Fix.FromInt(6);
        public const int SlowDurationFrames = 240;

        /// <summary>Captain Holdfast Pin (cast.md §5 / balance.md §4 max 75 f).</summary>
        public static readonly Fix HoldfastPinRange = Fix.FromInt(3);
        public const int PinDurationFrames = 75;
        public const int PinImmuneFrames = 240;

        /// <summary>Freeze / Petrify (balance.md §4 max 90 f).</summary>
        public static readonly Fix FreezeAreaRange = Fix.FromInt(4);
        public const int FreezeDurationFrames = 90;
        public const int FreezeImmuneFrames = 240;
        public const int FreezeDamageTakenPercent = 50;
        public const int FreezeBreakDamage = 100;

        /// <summary>
        /// S5 stub Adhesive Spray item id (<c>SampleContentFactory.AdhesiveSprayItemId</c>).
        /// Land / hit spawns a lasting Slow floor patch.
        /// </summary>
        public const int AdhesiveSprayItemId = 18;

        /// <summary>Legacy single-target Slow duration (still used by direct ApplySlow callers).</summary>
        public const int AdhesiveSlowFrames = 120;

        /// <summary>Adhesive Spray patch radius.</summary>
        public static readonly Fix AdhesiveSlowRange = Fix.FromInt(2);

        /// <summary>Adhesive Spray lasting patch lifetime (2 s).</summary>
        public const int AdhesivePatchLifetimeFrames = 120;

        /// <summary>
        /// S5 stub Petrify Gas item id (<c>SampleContentFactory.PetrifyGasItemId</c>).
        /// Thrown hit applies Freeze.
        /// </summary>
        public const int PetrifyGasItemId = 19;

        /// <summary>
        /// S5 stub Smoke Jar item id (<c>SampleContentFactory.SmokeJarItemId</c>).
        /// Land / hit spawns a lasting Blind floor patch.
        /// </summary>
        public const int SmokeJarItemId = 20;

        /// <summary>Smoke Jar patch radius.</summary>
        public static readonly Fix SmokeJarBlindRange = Fix.FromInt(3);

        /// <summary>Smoke Jar lasting patch lifetime (4 s).</summary>
        public const int SmokePatchLifetimeFrames = 240;

        /// <summary>
        /// S5 stub Ice Jar item id (<c>SampleContentFactory.IceJarItemId</c>).
        /// Land / hit spawns a lasting Freeze floor patch.
        /// </summary>
        public const int IceJarItemId = 21;

        /// <summary>Vein Guard stage-weapon item ids (SampleContentFactory mirrors).</summary>
        public const int StageMortarItemId = 26;
        public const int StageFrostStaffItemId = 27;
        public const int StageMalletItemId = 28;
        public const int StageNetItemId = 29;
        public const int StageFlameItemId = 30;

        /// <summary>Ice Jar patch radius.</summary>
        public static readonly Fix IceJarFreezeRange = Fix.FromInt(2);

        /// <summary>Ice Jar lasting patch lifetime (3 s).</summary>
        public const int IcePatchLifetimeFrames = 180;

        /// <summary>Status refresh while standing in a world patch.</summary>
        public const int WorldPatchStatusPulseFrames = 30;

        /// <summary>Hard-control cap for stun/dizzy/freeze/pin/grab sources (balance.md §4).</summary>
        public const int HardControlCapFrames = 120;

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
