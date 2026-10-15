using System;
using System.Runtime.InteropServices;

namespace JewelBlitzArena.Sim
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FixVec3 : IEquatable<FixVec3>
    {
        public readonly Fix X;
        public readonly Fix Y;
        public readonly Fix Z;

        public FixVec3(Fix x, Fix y, Fix z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static FixVec3 Zero => new FixVec3(Fix.Zero, Fix.Zero, Fix.Zero);

        public static FixVec3 operator +(FixVec3 a, FixVec3 b) =>
            new FixVec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static FixVec3 operator -(FixVec3 a, FixVec3 b) =>
            new FixVec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static FixVec3 operator *(FixVec3 a, Fix s) =>
            new FixVec3(a.X * s, a.Y * s, a.Z * s);

        public bool Equals(FixVec3 other) => X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object? obj) => obj is FixVec3 other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int h = X.GetHashCode();
                h = (h * 397) ^ Y.GetHashCode();
                return (h * 397) ^ Z.GetHashCode();
            }
        }
    }
}
