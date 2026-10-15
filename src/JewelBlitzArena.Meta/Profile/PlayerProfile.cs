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
                Flags = 0,
                // Fresh profile: Luster timer rule is "unlocked" as the default-editable row 3 data stub.
                UnlockedHouseRulesBits = HouseRulesBits.LusterTimer,
                HouseRulesDefaults = HouseRulesSettings.CreateDefaults(),
            };
            return profile;
        }
    }
}
