namespace JewelBlitzArena.Data
{
    /// <summary>Resolved fighter kit ready for Sim / Meta consumption.</summary>
    public sealed class FighterKitRecord
    {
        public int FighterId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SpeedRating { get; set; }
        public int LifeRating { get; set; }
        public int MaxLife { get; set; }
        public int ReachRating { get; set; }
        public int AirRating { get; set; }
        public int PowerRating { get; set; }

        /// <summary>Q48.16 metres/second for full stick.</summary>
        public long WalkSpeedRaw { get; set; }
        public long RunSpeedRaw { get; set; }
        public long FullSpeedRaw { get; set; }

        public MoveRecord[] Moves { get; set; } = System.Array.Empty<MoveRecord>();
    }

    public sealed class MoveRecord
    {
        public string Name { get; set; } = string.Empty;
        public MoveClass Class { get; set; }
        public int Damage { get; set; }
        public int StartupFrames { get; set; }
        public int ActiveFrames { get; set; }
        public int RecoveryFrames { get; set; }
        public bool UtilityExempt { get; set; }
    }

    public sealed class StageRecord
    {
        public int StageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public StagePhaseRecord[] Phases { get; set; } = System.Array.Empty<StagePhaseRecord>();

        /// <summary>Default jewel crate chance in percent (architecture / items §2).</summary>
        public int JewelCrateChancePercent { get; set; } = 8;

        public bool FallIsKo { get; set; }

        /// <summary>0 = use MatchRules default from actor count.</summary>
        public int JewelCapOverride { get; set; }
    }

    public sealed class StagePhaseRecord
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Duration in frames at 60 Hz. 0 = until match end.</summary>
        public int DurationFrames { get; set; }

        public int GeometrySetId { get; set; }
        public bool BlitzSuppressed { get; set; }

        /// <summary>Scripted enter-transition length (0 = instant). Hazards off during this.</summary>
        public int TransitionFrames { get; set; }

        public CarryItemsMode CarryItems { get; set; } = CarryItemsMode.All;
        public CarryJewelsMode CarryJewels { get; set; } = CarryJewelsMode.All;

        /// <summary>Permille (0–1000) when <see cref="CarryJewels"/> is DropChance.</summary>
        public int CarryJewelsDropPermille { get; set; }

        /// <summary>Camera volume center/radius as Q48.16 — Presentation only.</summary>
        public long CameraCenterXRaw { get; set; }
        public long CameraCenterYRaw { get; set; }
        public long CameraCenterZRaw { get; set; }
        public long CameraRadiusRaw { get; set; }

        public StageHazardRecord[] Hazards { get; set; } = System.Array.Empty<StageHazardRecord>();
        public StageKinematicRecord[] Kinematics { get; set; } = System.Array.Empty<StageKinematicRecord>();
    }

    public sealed class StageHazardRecord
    {
        public int HazardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public HazardTriggerKind Trigger { get; set; }
        public int PeriodFrames { get; set; }
        public int TelegraphFrames { get; set; }
        public int Damage { get; set; }
        public long CenterXRaw { get; set; }
        public long CenterYRaw { get; set; }
        public long CenterZRaw { get; set; }
        public long RadiusRaw { get; set; }
    }

    public sealed class StageKinematicRecord
    {
        public int TrackId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int PeriodFrames { get; set; }
        public long AmplitudeRaw { get; set; }
        public long OriginXRaw { get; set; }
        public long OriginYRaw { get; set; }
        public long OriginZRaw { get; set; }
    }

    public sealed class ItemRecord
    {
        public int ItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ItemCategory Category { get; set; }
        public int Rarity { get; set; }
        public CrateEligibility CrateEligibility { get; set; }
        public bool Pocketable { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; }
        public int DamagePerUse { get; set; }
        public int HealPerUse { get; set; }
    }
}
