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
    }

    public sealed class AuthoredStage
    {
        public int StageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int JewelCrateChancePercent { get; set; } = 8;
        public List<AuthoredPhase> Phases { get; set; } = new List<AuthoredPhase>();
    }

    public sealed class AuthoredPhase
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>Seconds as decimal string; converted to frames at 60 Hz on export.</summary>
        public string DurationSeconds { get; set; } = "0";
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
