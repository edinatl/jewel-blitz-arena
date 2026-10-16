namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Distinct Tournament clear → unlock ladder (modes.md §8).
    /// House Rules 1–7 unlock in order on clears 1–7; later clears set profile flags.
    /// Extra Arena slots 1–4 land at clears 9 / 11 / 13 / 14 (Cascade needs full cast).
    /// </summary>
    public static class UnlockLadderRuntime
    {
        public const int MaxLadderClears = 14;

        /// <summary>House Rules unlock order (combat-and-match.md §11 / modes.md §8).</summary>
        public static readonly int[] HouseRuleUnlockOrder =
        {
            HouseRulesBits.PocketItems,
            HouseRulesBits.JewelCapOverride,
            HouseRulesBits.LusterTimer,
            HouseRulesBits.BlitzedDamageTaken,
            HouseRulesBits.BlitzedAttack,
            HouseRulesBits.RallySeconds,
            HouseRulesBits.PhaseClock,
        };

        public static int HouseRulesBitsForClears(int distinctClears)
        {
            int n = distinctClears;
            if (n < 0)
            {
                n = 0;
            }

            if (n > HouseRuleUnlockOrder.Length)
            {
                n = HouseRuleUnlockOrder.Length;
            }

            int bits = 0;
            for (int i = 0; i < n; i++)
            {
                bits |= HouseRuleUnlockOrder[i];
            }

            return bits;
        }

        public static bool IsHouseRuleUnlocked(int distinctClears, int ruleBit) =>
            (HouseRulesBitsForClears(distinctClears) & ruleBit) != 0;

        public static bool IsCaptainCitrineUnlocked(int distinctClears) => distinctClears >= 7;

        public static bool IsSoundTestUnlocked(int distinctClears) => distinctClears >= 8;

        public static bool IsTillySetterUnlocked(int distinctClears) => distinctClears >= 8;

        public static bool IsTerracesExtraUnlocked(int distinctClears) => distinctClears >= 9;

        public static bool IsBossRushPresetUnlocked(int distinctClears) => distinctClears >= 10;

        public static bool IsBehindCounterUnlocked(int distinctClears) => distinctClears >= 11;

        public static bool IsRemixOptionUnlocked(int distinctClears) => distinctClears >= 12;

        public static bool IsSpirewalkUnlocked(int distinctClears) => distinctClears >= 13;

        /// <summary>
        /// Cascade / Extra Arena slot 4: clear 14 and full cast (modes.md §8).
        /// </summary>
        public static bool IsCascadeUnlocked(int distinctClears, bool ownedFullCast) =>
            distinctClears >= 14 && ownedFullCast;

        /// <summary>
        /// Extra Arena box slots unlocked for Custom (0–4). Slot 4 needs full cast.
        /// </summary>
        public static int ExtraArenaSlotCount(int distinctClears, bool ownedFullCast)
        {
            if (distinctClears < 9)
            {
                return 0;
            }

            if (distinctClears < 11)
            {
                return 1;
            }

            if (distinctClears < 13)
            {
                return 2;
            }

            if (distinctClears < 14 || !ownedFullCast)
            {
                return 3;
            }

            return 4;
        }

        public static int ExtraArenaSlotCount(PlayerProfile profile)
        {
            if (profile == null)
            {
                return 0;
            }

            return ExtraArenaSlotCount(
                profile.DistinctTournamentClears,
                profile.HasFlag(ProfileFlags.OwnedFullCast));
        }

        /// <summary>
        /// Record one distinct Tournament clear and sync House Rules + ladder flags.
        /// </summary>
        public static void ApplyDistinctTournamentClear(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.DistinctTournamentClears < int.MaxValue)
            {
                profile.DistinctTournamentClears++;
            }

            SyncUnlocks(profile);
        }

        /// <summary>Recompute unlock bits/flags from <see cref="PlayerProfile.DistinctTournamentClears"/>.</summary>
        public static void SyncUnlocks(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            int clears = profile.DistinctTournamentClears;
            bool fullCast = profile.HasFlag(ProfileFlags.OwnedFullCast);

            profile.UnlockedHouseRulesBits = HouseRulesBitsForClears(clears);
            profile.SetFlag(ProfileFlags.UnlockedCaptainCitrine, IsCaptainCitrineUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedSoundTest, IsSoundTestUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedTillySetter, IsTillySetterUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedTerracesExtra, IsTerracesExtraUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedBossRushPreset, IsBossRushPresetUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedBehindCounter, IsBehindCounterUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedRemixOption, IsRemixOptionUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedSpirewalk, IsSpirewalkUnlocked(clears));
            profile.SetFlag(ProfileFlags.UnlockedCascade, IsCascadeUnlocked(clears, fullCast));
        }
    }
}
