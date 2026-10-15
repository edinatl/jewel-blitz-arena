using System;
using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FixVec2 : IEquatable<FixVec2>
    {
        public readonly Fix X;
        public readonly Fix Y;

        public FixVec2(Fix x, Fix y)
        {
            X = x;
            Y = y;
        }

        public static FixVec2 Zero => new FixVec2(Fix.Zero, Fix.Zero);

        public static FixVec2 operator +(FixVec2 a, FixVec2 b) => new FixVec2(a.X + b.X, a.Y + b.Y);

        public static FixVec2 operator -(FixVec2 a, FixVec2 b) => new FixVec2(a.X - b.X, a.Y - b.Y);

        public static FixVec2 operator *(FixVec2 a, Fix s) => new FixVec2(a.X * s, a.Y * s);

        public bool Equals(FixVec2 other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is FixVec2 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }
    }
}
