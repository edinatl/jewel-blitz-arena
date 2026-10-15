using System.Runtime.CompilerServices;

namespace JewelBlitzArena.Data
{
    /// <summary>xxHash64 over bytes for content-blob hashes (lobby handshake).</summary>
    public static unsafe class ContentXxHash64
    {
        private const ulong Prime1 = 11400714785074694791UL;
        private const ulong Prime2 = 14029467366897019727UL;
        private const ulong Prime3 = 1609587929392839161UL;
        private const ulong Prime4 = 9650029242287828579UL;
        private const ulong Prime5 = 2870177450012600261UL;

        public static ulong Hash(byte[] data, ulong seed = 0UL)
        {
            fixed (byte* p = data)
            {
                return Hash(p, data.Length, seed);
            }
        }

        public static ulong Hash(byte* data, int length, ulong seed = 0UL)
        {
            byte* p = data;
            byte* end = p + length;
            ulong hash;

            if (length >= 32)
            {
                byte* limit = end - 32;
                ulong v1 = seed + Prime1 + Prime2;
                ulong v2 = seed + Prime2;
                ulong v3 = seed + 0;
                ulong v4 = seed - Prime1;

                do
                {
                    v1 = Round(v1, ReadU64(p)); p += 8;
                    v2 = Round(v2, ReadU64(p)); p += 8;
                    v3 = Round(v3, ReadU64(p)); p += 8;
                    v4 = Round(v4, ReadU64(p)); p += 8;
                }
                while (p <= limit);

                hash = Rotl(v1, 1) + Rotl(v2, 7) + Rotl(v3, 12) + Rotl(v4, 18);
                hash = MergeRound(hash, v1);
                hash = MergeRound(hash, v2);
                hash = MergeRound(hash, v3);
                hash = MergeRound(hash, v4);
            }
            else
            {
                hash = seed + Prime5;
            }

            hash += (ulong)length;

            while (p + 8 <= end)
            {
                ulong k1 = Round(0, ReadU64(p));
                hash ^= k1;
                hash = Rotl(hash, 27) * Prime1 + Prime4;
                p += 8;
            }

            if (p + 4 <= end)
            {
                hash ^= ReadU32(p) * Prime1;
                hash = Rotl(hash, 23) * Prime2 + Prime3;
                p += 4;
            }

            while (p < end)
            {
                hash ^= *p * Prime5;
                hash = Rotl(hash, 11) * Prime1;
                p++;
            }

            hash ^= hash >> 33;
            hash *= Prime2;
            hash ^= hash >> 29;
            hash *= Prime3;
            hash ^= hash >> 32;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Round(ulong acc, ulong input)
        {
            acc += input * Prime2;
            acc = Rotl(acc, 31);
            acc *= Prime1;
            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MergeRound(ulong acc, ulong val)
        {
            val = Round(0, val);
            acc ^= val;
            acc = acc * Prime1 + Prime4;
            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Rotl(ulong x, int r) => (x << r) | (x >> (64 - r));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadU64(byte* p) =>
            (ulong)p[0] | ((ulong)p[1] << 8) | ((ulong)p[2] << 16) | ((ulong)p[3] << 24)
            | ((ulong)p[4] << 32) | ((ulong)p[5] << 40) | ((ulong)p[6] << 48) | ((ulong)p[7] << 56);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadU32(byte* p) =>
            (ulong)p[0] | ((ulong)p[1] << 8) | ((ulong)p[2] << 16) | ((ulong)p[3] << 24);
    }
}
