using System;
using System.Globalization;

namespace JewelBlitzArena.Data
{
    /// <summary>
    /// Parses authored decimal strings into Q48.16 raw longs without using <c>float</c>/<c>double</c>.
    /// See <c>docs/tech/architecture.md</c> §4.
    /// </summary>
    public static class DecimalFixParser
    {
        public const int FractionBits = 16;
        public const long OneRaw = 1L << FractionBits;

        public static long ParseToRaw(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Empty fixed-point decimal string.");
            }

            text = text.Trim();
            bool negative = false;
            if (text[0] == '-')
            {
                negative = true;
                text = text.Substring(1);
            }
            else if (text[0] == '+')
            {
                text = text.Substring(1);
            }

            int dot = text.IndexOf('.');
            string intPart = dot >= 0 ? text.Substring(0, dot) : text;
            string fracPart = dot >= 0 ? text.Substring(dot + 1) : string.Empty;

            if (intPart.Length == 0)
            {
                intPart = "0";
            }

            if (!ulong.TryParse(intPart, NumberStyles.None, CultureInfo.InvariantCulture, out ulong intValue))
            {
                throw new FormatException($"Invalid integer part in '{text}'.");
            }

            long raw = (long)intValue * OneRaw;

            if (fracPart.Length > 0)
            {
                // Cap fractional digits; extra precision is truncated toward zero after scale.
                if (fracPart.Length > 18)
                {
                    fracPart = fracPart.Substring(0, 18);
                }

                if (!ulong.TryParse(fracPart, NumberStyles.None, CultureInfo.InvariantCulture, out ulong fracValue))
                {
                    throw new FormatException($"Invalid fractional part in '{text}'.");
                }

                ulong denom = 1UL;
                for (int i = 0; i < fracPart.Length; i++)
                {
                    denom *= 10UL;
                }

                // frac * 65536 / 10^n  (integer division, toward zero).
                raw += (long)((fracValue * (ulong)OneRaw) / denom);
            }

            return negative ? -raw : raw;
        }

        public static long ParseToRawOrDefault(string? text, long defaultRaw)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return defaultRaw;
            }

            return ParseToRaw(text);
        }
    }
}
