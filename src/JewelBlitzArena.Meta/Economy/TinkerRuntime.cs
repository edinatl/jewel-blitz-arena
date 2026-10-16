namespace JewelBlitzArena.Meta
{
    /// <summary>Tinker craft outcome (items-and-economy.md §4).</summary>
    public enum TinkerOutcome : byte
    {
        Failure = 0,
        Success = 1,
        Double = 2,
    }

    /// <summary>Result of one greybox Tinker attempt.</summary>
    public readonly struct TinkerAttemptResult
    {
        public TinkerAttemptResult(
            TinkerOutcome outcome,
            int resultItemId,
            int chitsEarned,
            bool scrapGranted,
            int counterUsed)
        {
            Outcome = outcome;
            ResultItemId = resultItemId;
            ChitsEarned = chitsEarned;
            ScrapGranted = scrapGranted;
            CounterUsed = counterUsed;
        }

        public TinkerOutcome Outcome { get; }
        public int ResultItemId { get; }
        public int ChitsEarned { get; }
        public bool ScrapGranted { get; }
        public int CounterUsed { get; }
    }

    /// <summary>
    /// Tilly's Bench Tinker greybox: binary mix → success/double/failure.
    /// Failure pays Scrap + Chit (Facet Wheel earn path). Seeded by
    /// hash(profile_salt, tinker_counter, A, B, card); counter increments before result.
    /// </summary>
    public static class TinkerRuntime
    {
        public const int ScrapItemId = 900101;
        public const int SuccessPercent = 55;
        public const int DoublePercent = 15;
        public const int FailurePercent = 30;
        public const int DoubleExtraSellCarats = 200;
        public const int ResultItemBaseId = 930000;

        /// <summary>Ensure a non-zero profile salt for deterministic craft seeds.</summary>
        public static void EnsureSalt(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (profile.ProfileSalt != 0UL)
            {
                return;
            }

            // Stable derivation from display name so CreateNew is reproducible in tests
            // that set DisplayName; otherwise a fixed greybox default.
            ulong salt = 0x54494C4C59534C54UL; // TILLYSLT
            string name = profile.DisplayName ?? string.Empty;
            for (int i = 0; i < name.Length; i++)
            {
                salt = Mix(salt ^ (ulong)(uint)name[i] << (i & 15));
            }

            profile.ProfileSalt = salt == 0UL ? 1UL : salt;
        }

        public static ulong CraftSeed(ulong profileSalt, int tinkerCounter, int inputA, int inputB, int card)
        {
            ulong x = profileSalt;
            x ^= (ulong)(uint)tinkerCounter * 0x9E3779B97F4A7C15UL;
            x ^= (ulong)(uint)inputA << 1;
            x ^= (ulong)(uint)inputB << 17;
            x ^= (ulong)(uint)card << 33;
            return Mix(x);
        }

        public static TinkerOutcome ResolveOutcome(ulong craftSeed)
        {
            int roll = (int)(craftSeed % 100UL);
            if (roll < SuccessPercent)
            {
                return TinkerOutcome.Success;
            }

            if (roll < SuccessPercent + DoublePercent)
            {
                return TinkerOutcome.Double;
            }

            return TinkerOutcome.Failure;
        }

        public static int ResultItemIdFor(int inputA, int inputB, int card) =>
            ResultItemBaseId + (((inputA * 31) + (inputB * 17) + (card * 13)) & 0xFF);

        /// <summary>
        /// Attempt a craft. Increments <see cref="PlayerProfile.TinkerCounter"/> before
        /// resolving so a reload cannot reroll the same counter (economy §4).
        /// </summary>
        public static TinkerAttemptResult TryCraft(
            PlayerProfile profile,
            int inputA,
            int inputB,
            int card = 0)
        {
            if (profile == null)
            {
                return new TinkerAttemptResult(TinkerOutcome.Failure, 0, 0, false, 0);
            }

            EnsureSalt(profile);
            int counterUsed = profile.TinkerCounter;
            if (profile.TinkerCounter < int.MaxValue)
            {
                profile.TinkerCounter++;
            }

            profile.SetFlag(ProfileFlags.TouchedTinkerStub);

            ulong seed = CraftSeed(profile.ProfileSalt, counterUsed, inputA, inputB, card);
            TinkerOutcome outcome = ResolveOutcome(seed);

            if (outcome == TinkerOutcome.Failure)
            {
                if (profile.Chits < int.MaxValue)
                {
                    profile.Chits++;
                }

                profile.Ledger.Grant(ScrapItemId);
                return new TinkerAttemptResult(outcome, 0, chitsEarned: 1, scrapGranted: true, counterUsed);
            }

            int resultId = ResultItemIdFor(inputA, inputB, card);
            bool firstGrant = profile.Ledger.Grant(resultId);
            if (outcome == TinkerOutcome.Double)
            {
                // Capacity-1 extras pay sell price in carats (greybox flat).
                if (!firstGrant || !profile.Ledger.Grant(resultId))
                {
                    long sum = (long)profile.Carats + DoubleExtraSellCarats;
                    profile.Carats = sum > int.MaxValue ? int.MaxValue : (int)sum;
                }
            }

            return new TinkerAttemptResult(
                outcome, resultId, chitsEarned: 0, scrapGranted: false, counterUsed);
        }

        private static ulong Mix(ulong x)
        {
            x ^= x >> 30;
            x *= 0xBF58476D1CE4E5B9UL;
            x ^= x >> 27;
            x *= 0x94D049BB133111EBUL;
            x ^= x >> 31;
            return x;
        }
    }
}
