using System;
using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Q48.16 fixed-point (16 fractional bits). Backed by <see cref="long"/>.
    /// See <c>docs/tech/architecture.md</c> §2.1 (D2).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct Fix : IEquatable<Fix>, IComparable<Fix>
    {
        public const int FractionBits = 16;
        public const long OneRaw = 1L << FractionBits;

        public static readonly Fix Zero = new Fix(0);
        public static readonly Fix One = new Fix(OneRaw);
        public static readonly Fix NegOne = new Fix(-OneRaw);

        public readonly long Raw;

        public Fix(long raw)
        {
            Raw = raw;
        }

        public static Fix FromInt(int value) => new Fix(value * OneRaw);

        public static Fix FromRaw(long raw) => new Fix(raw);

        /// <summary>Nearest integer toward −∞ (floor of the real value).</summary>
        public int ToIntFloor() => (int)(Raw >> FractionBits);

        public static Fix operator +(Fix a, Fix b) => new Fix(a.Raw + b.Raw);

        public static Fix operator -(Fix a, Fix b) => new Fix(a.Raw - b.Raw);

        public static Fix operator -(Fix a) => new Fix(-a.Raw);

        /// <summary>
        /// Multiply. Safe for typical gameplay magnitudes (|integer part| ≲ 46_000)
        /// without a 128-bit intermediate. Prefer <see cref="MulWide"/> when unsure.
        /// </summary>
        public static Fix operator *(Fix a, Fix b) => new Fix((a.Raw * b.Raw) >> FractionBits);

        /// <summary>Scale by a small integer (no fractional rounding).</summary>
        public static Fix operator *(Fix a, int b) => new Fix(a.Raw * b);

        public static Fix operator *(int a, Fix b) => new Fix(a * b.Raw);

        public static Fix operator /(Fix a, int b)
        {
            if (b == 0)
            {
                throw new DivideByZeroException("Fix divide by zero.");
            }

            return new Fix(a.Raw / b);
        }

        /// <summary>
        /// Wide multiply using a software 128-bit product, then arithmetic shift by 16.
        /// </summary>
        public static Fix MulWide(Fix a, Fix b)
        {
            Mul64To128(a.Raw, b.Raw, out long hi, out ulong lo);
            return new Fix(SignedShiftRight128(hi, lo, FractionBits));
        }

        public static bool operator ==(Fix a, Fix b) => a.Raw == b.Raw;

        public static bool operator !=(Fix a, Fix b) => a.Raw != b.Raw;

        public static bool operator <(Fix a, Fix b) => a.Raw < b.Raw;

        public static bool operator >(Fix a, Fix b) => a.Raw > b.Raw;

        public static bool operator <=(Fix a, Fix b) => a.Raw <= b.Raw;

        public static bool operator >=(Fix a, Fix b) => a.Raw >= b.Raw;

        public bool Equals(Fix other) => Raw == other.Raw;

        public override bool Equals(object? obj) => obj is Fix other && Equals(other);

        public override int GetHashCode() => Raw.GetHashCode();

        public int CompareTo(Fix other) => Raw.CompareTo(other.Raw);

        public override string ToString() => $"Fix({Raw}/{OneRaw})";

        private static void Mul64To128(long a, long b, out long hi, out ulong lo)
        {
            bool negative = (a < 0) ^ (b < 0);
            ulong ua = (ulong)(a < 0 ? -a : a);
            ulong ub = (ulong)(b < 0 ? -b : b);

            ulong aLo = (uint)ua;
            ulong aHi = ua >> 32;
            ulong bLo = (uint)ub;
            ulong bHi = ub >> 32;

            ulong p0 = aLo * bLo;
            ulong p1 = aLo * bHi;
            ulong p2 = aHi * bLo;
            ulong p3 = aHi * bHi;

            ulong mid = (p0 >> 32) + (uint)p1 + (uint)p2;
            ulong loResult = (p0 & 0xFFFFFFFFUL) | (mid << 32);
            ulong hiResult = p3 + (p1 >> 32) + (p2 >> 32) + (mid >> 32);

            if (negative)
            {
                loResult = ~loResult + 1UL;
                hiResult = ~hiResult;
                if (loResult == 0UL)
                {
                    hiResult += 1UL;
                }
            }

            lo = loResult;
            hi = (long)hiResult;
        }

        private static long SignedShiftRight128(long hi, ulong lo, int shift)
        {
            ulong newLo = (lo >> shift) | ((ulong)hi << (64 - shift));
            return (long)newLo;
        }
    }
}
