using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// xoshiro256** — deterministic RNG living inside <see cref="MatchState"/>.
    /// Spec: https://prng.di.unimi.it/xoshiro256starstar.c
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Xoshiro256StarStar
    {
        public ulong S0;
        public ulong S1;
        public ulong S2;
        public ulong S3;

        public static int SizeOf => sizeof(ulong) * 4;

        /// <summary>Seed all four words from a single 64-bit seed (SplitMix64 expansion).</summary>
        public void Seed(ulong seed)
        {
            S0 = SplitMix64(ref seed);
            S1 = SplitMix64(ref seed);
            S2 = SplitMix64(ref seed);
            S3 = SplitMix64(ref seed);

            // xoshiro requires a non-all-zero state.
            if ((S0 | S1 | S2 | S3) == 0UL)
            {
                S0 = 0x9E3779B97F4A7C15UL;
            }
        }

        public ulong NextUInt64()
        {
            ulong result = Rotl(S1 * 5UL, 7) * 9UL;
            ulong t = S1 << 17;

            S2 ^= S0;
            S3 ^= S1;
            S1 ^= S2;
            S0 ^= S3;

            S2 ^= t;
            S3 = Rotl(S3, 45);
            return result;
        }

        /// <summary>Uniform in [0, maxExclusive). Rejection sampling — no float bias.</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                return 0;
            }

            ulong limit = (ulong)maxExclusive;
            ulong threshold = (0UL - limit) % limit;
            ulong value;
            do
            {
                value = NextUInt64();
            }
            while (value < threshold);

            return (int)(value % limit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        private static ulong SplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
