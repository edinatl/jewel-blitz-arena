using System;

namespace JewelBlitzArena.Meta
{
    /// <summary>Forward-only profile migrations. v0 → v1 is a no-op structure fill.</summary>
    public static class ProfileMigrator
    {
        public static PlayerProfile MigrateToCurrent(PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            while (profile.SchemaVersion < ProfileSchema.Current)
            {
                switch (profile.SchemaVersion)
                {
                    case ProfileSchema.Version0:
                        MigrateV0ToV1(profile);
                        break;
                    case ProfileSchema.Version1:
                        MigrateV1ToV2(profile);
                        break;
                    case ProfileSchema.Version2:
                        MigrateV2ToV3(profile);
                        break;
                    case ProfileSchema.Version3:
                        MigrateV3ToV4(profile);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown profile schema {profile.SchemaVersion}; cannot migrate.");
                }
            }

            return profile;
        }

        /// <summary>
        /// v0 was an empty placeholder. Filling defaults is a structural no-op for gameplay data.
        /// </summary>
        private static void MigrateV0ToV1(PlayerProfile profile)
        {
            if (string.IsNullOrEmpty(profile.DisplayName))
            {
                profile.DisplayName = "Rookie";
            }

            if (profile.HouseRulesDefaults.BlitzedDamageTakenPercent == 0
                && profile.HouseRulesDefaults.BlitzedAttackPercent == 0
                && profile.HouseRulesDefaults.RallySeconds == 0
                && profile.HouseRulesDefaults.PhaseClockPercent == 0)
            {
                profile.HouseRulesDefaults = HouseRulesSettings.CreateDefaults();
            }

            // Unlock bits come from DistinctTournamentClears via UnlockLadder (modes.md §8).
            profile.UnlockedHouseRulesBits =
                UnlockLadderRuntime.HouseRulesBitsForClears(profile.DistinctTournamentClears);

            profile.SchemaVersion = ProfileSchema.Version1;
        }

        private static void MigrateV1ToV2(PlayerProfile profile)
        {
            if (profile.BestExpeditionDifficulty < 0)
            {
                profile.BestExpeditionDifficulty = 0;
            }

            profile.SchemaVersion = ProfileSchema.Version2;
        }

        private static void MigrateV2ToV3(PlayerProfile profile)
        {
            if (profile.Chits < 0)
            {
                profile.Chits = 0;
            }

            if (profile.FacetWheelWidthLevel < 0)
            {
                profile.FacetWheelWidthLevel = 0;
            }

            if (profile.FacetWheelWidthLevel > FacetWheelRuntime.MaxWidthLevel)
            {
                profile.FacetWheelWidthLevel = FacetWheelRuntime.MaxWidthLevel;
            }

            profile.SchemaVersion = ProfileSchema.Version3;
        }

        private static void MigrateV3ToV4(PlayerProfile profile)
        {
            if (profile.PlaytimeSeconds < 0)
            {
                profile.PlaytimeSeconds = 0;
            }

            if (profile.CaratsSpentLifetime < 0)
            {
                profile.CaratsSpentLifetime = 0;
            }

            profile.SchemaVersion = ProfileSchema.Version4;
        }
    }
}
