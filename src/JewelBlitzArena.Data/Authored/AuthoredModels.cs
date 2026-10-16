using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    /// <summary>
    /// Designer-facing pack. Fixed-point fields are decimal <b>strings</b> (never float JSON numbers).
    /// </summary>
    public sealed class AuthoredContentPack
    {
        public uint SchemaVersion { get; set; } = ContentSchema.Version;
        public List<AuthoredFighter> Fighters { get; set; } = new List<AuthoredFighter>();
        public List<AuthoredStage> Stages { get; set; } = new List<AuthoredStage>();
        public List<AuthoredItem> Items { get; set; } = new List<AuthoredItem>();
    }

    public sealed class AuthoredFighter
    {
        public int FighterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SpeedRating { get; set; } = 3;
        public int LifeRating { get; set; } = 3;
        public int ReachRating { get; set; } = 3;
        public int AirRating { get; set; } = 3;
        public int PowerRating { get; set; } = 3;

        /// <summary>Decimal strings, metres/second.</summary>
        public string WalkSpeed { get; set; } = "3";
        public string RunSpeed { get; set; } = "5";
        public string FullSpeed { get; set; } = "7";

        /// <summary>
        /// Named kit exception (architecture §2.5): None, VanishStrong, CommandGrabCarry,
        /// RiposteStrong, ExtendedDodge, TripleJumpGlide, DoubleHold, SwapPartners, InvoiceRepeatable.
        /// </summary>
        public string KitBehavior { get; set; } = "None";

        public List<AuthoredMove> Moves { get; set; } = new List<AuthoredMove>();
    }

    public sealed class AuthoredMove
    {
        public string Name { get; set; } = string.Empty;
        public string Class { get; set; } = "Attack";
        public int Damage { get; set; }
        public int StartupFrames { get; set; }
        public int ActiveFrames { get; set; }
        public int RecoveryFrames { get; set; }
        public bool UtilityExempt { get; set; }

        /// <summary>None, Heal, Magnet, Utility, Stun, Blind, Dizzy, Slow, Pin, or Freeze.</summary>
        public string Effect { get; set; } = "None";
    }

    public sealed class AuthoredStage
    {
        public int StageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int JewelCrateChancePercent { get; set; } = 8;
        public bool FallIsKo { get; set; }
        public int JewelCapOverride { get; set; }
        public List<AuthoredPhase> Phases { get; set; } = new List<AuthoredPhase>();
    }

    public sealed class AuthoredPhase
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Seconds as decimal string; converted to frames at 60 Hz on export.</summary>
        public string DurationSeconds { get; set; } = "0";

        public int GeometrySetId { get; set; }
        public bool BlitzSuppressed { get; set; }

        /// <summary>Enter-transition length in seconds (decimal string).</summary>
        public string TransitionSeconds { get; set; } = "0";

        /// <summary>all | held_only | none</summary>
        public string CarryItems { get; set; } = "all";

        /// <summary>all | drop_chance | none</summary>
        public string CarryJewels { get; set; } = "all";

        /// <summary>Decimal string probability when CarryJewels is drop_chance (e.g. "0.5").</summary>
        public string CarryJewelsDropChance { get; set; } = "0";

        public string CameraCenterX { get; set; } = "0";
        public string CameraCenterY { get; set; } = "8";
        public string CameraCenterZ { get; set; } = "0";
        public string CameraRadius { get; set; } = "12";

        /// <summary>
        /// Optional enter-transition camera path (`stages.md` scripted_camera).
        /// Null/absent → Presentation lerps previous→current phase volumes.
        /// Omitted start/end fields inherit previous/current phase camera volume.
        /// </summary>
        public AuthoredScriptedCamera? ScriptedCamera { get; set; }

        public List<AuthoredHazard> Hazards { get; set; } = new List<AuthoredHazard>();
        public List<AuthoredKinematic> Kinematics { get; set; } = new List<AuthoredKinematic>();

        /// <summary>Breakable crate positions (metres, decimal strings). Empty = none.</summary>
        public List<AuthoredCratePoint> CratePoints { get; set; } = new List<AuthoredCratePoint>();

        /// <summary>Persistent water volumes for this phase. Empty = none.</summary>
        public List<AuthoredWaterVolume> WaterVolumes { get; set; } = new List<AuthoredWaterVolume>();
    }

    public sealed class AuthoredCratePoint
    {
        public string X { get; set; } = "0";
        public string Z { get; set; } = "0";
    }

    public sealed class AuthoredWaterVolume
    {
        public string CenterX { get; set; } = "0";
        public string CenterZ { get; set; } = "0";
        public string Radius { get; set; } = "8";
    }

    /// <summary>Presentation-only scripted camera path for a phase enter-transition.</summary>
    public sealed class AuthoredScriptedCamera
    {
        /// <summary>When true (default if object present), Presentation uses this path during InTransition.</summary>
        public bool Enabled { get; set; } = true;

        public string? StartCenterX { get; set; }
        public string? StartCenterY { get; set; }
        public string? StartCenterZ { get; set; }
        public string? StartRadius { get; set; }

        public string? EndCenterX { get; set; }
        public string? EndCenterY { get; set; }
        public string? EndCenterZ { get; set; }
        public string? EndRadius { get; set; }
    }

    public sealed class AuthoredHazard
    {
        public int HazardId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>phase_enter | timer | proximity</summary>
        public string Trigger { get; set; } = "timer";

        public string PeriodSeconds { get; set; } = "8";
        public int TelegraphFrames { get; set; } = 40;
        public int Damage { get; set; }
        public string CenterX { get; set; } = "0";
        public string CenterY { get; set; } = "0";
        public string CenterZ { get; set; } = "0";
        public string Radius { get; set; } = "1.5";
    }

    public sealed class AuthoredKinematic
    {
        public int TrackId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PeriodSeconds { get; set; } = "8";
        public string Amplitude { get; set; } = "2";
        public string OriginX { get; set; } = "0";
        public string OriginY { get; set; } = "0";
        public string OriginZ { get; set; } = "0";
    }

    public sealed class AuthoredItem
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Melee";
        public int Rarity { get; set; } = 1;
        public string CrateEligibility { get; set; } = "Always";
        public bool Pocketable { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
        public int DamagePerUse { get; set; }
        public int HealPerUse { get; set; }
    }
}
