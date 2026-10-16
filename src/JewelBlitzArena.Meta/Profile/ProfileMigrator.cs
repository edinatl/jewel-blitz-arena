using System;
using JewelBlitzArena.Data;

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
                    case ProfileSchema.Version4:
                        MigrateV4ToV5(profile);
                        break;
                    case ProfileSchema.Version5:
                        MigrateV5ToV6(profile);
                        break;
                    case ProfileSchema.Version6:
                        MigrateV6ToV7(profile);
                        break;
                    case ProfileSchema.Version7:
                        MigrateV7ToV8(profile);
                        break;
                    case ProfileSchema.Version8:
                        MigrateV8ToV9(profile);
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

        private static void MigrateV4ToV5(PlayerProfile profile)
        {
            if (profile.TinkerCounter < 0)
            {
                profile.TinkerCounter = 0;
            }

            // Salt stays 0 until first Bench open / craft (EnsureSalt).
            profile.SchemaVersion = ProfileSchema.Version5;
        }

        private static void MigrateV5ToV6(PlayerProfile profile)
        {
            // BenchNotes starts empty for upgraded profiles.
            profile.SchemaVersion = ProfileSchema.Version6;
        }

        private static void MigrateV6ToV7(PlayerProfile profile)
        {
            if (profile.ShopStockSeed == 0UL)
            {
                profile.ShopStockSeed = 0x53484F5053544BUL;
            }

            profile.SchemaVersion = ProfileSchema.Version7;
        }

        private static void MigrateV7ToV8(PlayerProfile profile)
        {
            // Carry EquippedDiadem flag into Head socket when Diadem is owned.
            if (profile.HasFlag(ProfileFlags.EquippedDiadem)
                && profile.Ledger.Owns(CitrineGiftsRuntime.DiademItemId)
                && profile.Fitting.Get(CosmeticSocket.Head) == 0)
            {
                profile.Fitting.Set(CosmeticSocket.Head, CitrineGiftsRuntime.DiademItemId);
            }

            profile.SchemaVersion = ProfileSchema.Version8;
        }

        private static void MigrateV8ToV9(PlayerProfile profile)
        {
            // Pockets start empty for upgraded profiles.
            profile.SchemaVersion = ProfileSchema.Version9;
        }
    }
}
