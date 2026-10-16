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

            if (profile.UnlockedHouseRulesBits == 0)
            {
                profile.UnlockedHouseRulesBits = HouseRulesBits.LusterTimer;
            }

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
    }
}
