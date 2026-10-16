using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Versioned player save. Modes read/write this; Sim never sees a profile.
    /// </summary>
    public sealed class PlayerProfile
    {
        public int SchemaVersion { get; set; } = ProfileSchema.Current;

        public string DisplayName { get; set; } = "Rookie";

        public int Carats { get; set; }

        /// <summary>Distinct Tournament clears (any fighter) — gates House Rules unlocks later.</summary>
        public int DistinctTournamentClears { get; set; }

        /// <summary>
        /// Highest Expedition difficulty cleared (0 = never). Gates difficulty 7–8 after ≥5.
        /// </summary>
        public int BestExpeditionDifficulty { get; set; }

        /// <summary>Tinker failure Chits — Facet Wheel currency (items-and-economy.md §5).</summary>
        public int Chits { get; set; }

        /// <summary>Facet Wheel width level 0–5 (cumulative carat widen steps).</summary>
        public int FacetWheelWidthLevel { get; set; }

        /// <summary>Lifetime playtime in seconds — feeds Citrine gift appearance (economy §6).</summary>
        public int PlaytimeSeconds { get; set; }

        /// <summary>Lifetime carats spent in shop/widen — feeds Citrine gift appearance.</summary>
        public int CaratsSpentLifetime { get; set; }

        /// <summary><see cref="ProfileFlags"/> bits.</summary>
        public int Flags { get; set; }

        /// <summary>House Rules bits unlocked for editing in Custom (data only in S6).</summary>
        public int UnlockedHouseRulesBits { get; set; }

        /// <summary>Last Custom / Sparring House Rules selection persisted for lobby defaults.</summary>
        public HouseRulesSettings HouseRulesDefaults { get; set; } = HouseRulesSettings.CreateDefaults();

        public LedgerInventory Ledger { get; } = new LedgerInventory();

        public bool HasFlag(int flag) => (Flags & flag) != 0;

        public void SetFlag(int flag, bool value = true)
        {
            if (value)
            {
                Flags |= flag;
            }
            else
            {
                Flags &= ~flag;
            }
        }

        public static PlayerProfile CreateNew(string displayName = "Rookie")
        {
            var profile = new PlayerProfile
            {
                SchemaVersion = ProfileSchema.Current,
                DisplayName = displayName ?? "Rookie",
                Carats = 0,
                DistinctTournamentClears = 0,
                BestExpeditionDifficulty = 0,
                Chits = 0,
                FacetWheelWidthLevel = 0,
                PlaytimeSeconds = 0,
                CaratsSpentLifetime = 0,
                Flags = 0,
                // House Rules rows start locked; defaults still apply via HouseRulesDefaults.
                UnlockedHouseRulesBits = UnlockLadderRuntime.HouseRulesBitsForClears(0),
                HouseRulesDefaults = HouseRulesSettings.CreateDefaults(),
            };
            return profile;
        }
    }
}
