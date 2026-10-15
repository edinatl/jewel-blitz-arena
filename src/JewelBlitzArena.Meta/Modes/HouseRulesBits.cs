using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// House Rules bit layout for Meta / lobbies. Bit 0 matches <see cref="MatchRules.HouseLusterTimer"/>.
    /// Other bits are Meta data until Sim consumes them.
    /// </summary>
    public static class HouseRulesBits
    {
        public const int PocketItems = 1 << 1;
        public const int JewelCapOverride = 1 << 2; // presence of override; value in settings
        public const int LusterTimer = MatchRules.HouseLusterTimer; // 1 << 0
        public const int BlitzedDamageTaken = 1 << 3;
        public const int BlitzedAttack = 1 << 4;
        public const int RallySeconds = 1 << 5;
        public const int PhaseClock = 1 << 6;

        /// <summary>Bits Sim currently understands (pass through MatchConfig.HouseRulesBits).</summary>
        public static int ToSimBits(in HouseRulesSettings settings)
        {
            int bits = 0;
            if (settings.LusterTimer)
            {
                bits |= LusterTimer;
            }

            return bits;
        }
    }

    /// <summary>Editable House Rules values (Custom lobby). Defaults from combat-and-match.md §11.</summary>
    public struct HouseRulesSettings
    {
        public bool PocketItems;
        public int JewelCap; // 0 = Auto
        public bool LusterTimer;
        public int BlitzedDamageTakenPercent; // default 50
        public int BlitzedAttackPercent; // default 100
        public int RallySeconds; // default 9
        public int PhaseClockPercent; // default 100

        public static HouseRulesSettings CreateDefaults() => new HouseRulesSettings
        {
            PocketItems = false,
            JewelCap = 0,
            LusterTimer = true,
            BlitzedDamageTakenPercent = 50,
            BlitzedAttackPercent = 100,
            RallySeconds = 9,
            PhaseClockPercent = 100,
        };
    }
}
