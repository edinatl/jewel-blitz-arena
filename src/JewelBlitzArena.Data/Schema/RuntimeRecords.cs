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
    }

    public sealed class StagePhaseRecord
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Duration in frames at 60 Hz. 0 = until match end.</summary>
        public int DurationFrames { get; set; }
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
