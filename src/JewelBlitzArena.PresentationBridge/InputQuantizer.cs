using System;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-side stick/button quantization into <see cref="InputFrame"/>
    /// (architecture §3). Float math is allowed here; Sim only sees the 2-byte result.
    /// </summary>
    public static class InputQuantizer
    {
        public const float NeutralThreshold = 0.25f;
        public const float WalkThreshold = 0.55f;
        public const float RunThreshold = 0.80f;

        public static InputFrame FromAxes(
            float axisX,
            float axisZ,
            bool jump = false,
            bool attack = false,
            bool action = false,
            bool strong = false,
            bool surgeA = false,
            bool surgeB = false)
        {
            float mag = (float)Math.Sqrt((axisX * axisX) + (axisZ * axisZ));
            int magnitude = QuantizeMagnitude(mag);
            int direction = 0;
            if (magnitude > 0)
            {
                direction = QuantizeDirection(axisX, axisZ);
            }

            var frame = InputFrame.FromStick(direction, magnitude);
            frame.Jump = jump;
            frame.Attack = attack;
            frame.Action = action;
            frame.Strong = strong;
            frame.SurgeA = surgeA;
            frame.SurgeB = surgeB;
            return frame;
        }

        /// <summary>Keyboard / D-pad helper: each axis in [-1, 1].</summary>
        public static InputFrame FromCardinal(
            int x,
            int z,
            bool jump = false,
            bool attack = false,
            bool action = false,
            bool strong = false,
            bool surgeA = false,
            bool surgeB = false)
        {
            float fx = x < -1 ? -1f : (x > 1 ? 1f : x);
            float fz = z < -1 ? -1f : (z > 1 ? 1f : z);
            if (fx != 0f && fz != 0f)
            {
                // Normalize diagonals so magnitude still hits "full".
                float inv = 1f / (float)Math.Sqrt(2.0);
                fx *= inv;
                fz *= inv;
                // Bump slightly so diagonal passes RunThreshold after normalize.
                fx *= 1.01f;
                fz *= 1.01f;
            }

            return FromAxes(fx, fz, jump, attack, action, strong, surgeA, surgeB);
        }

        public static int QuantizeMagnitude(float magnitude)
        {
            float m = magnitude < 0f ? -magnitude : magnitude;
            if (m < NeutralThreshold)
            {
                return 0;
            }

            if (m < WalkThreshold)
            {
                return 1;
            }

            if (m < RunThreshold)
            {
                return 2;
            }

            return 3;
        }

        /// <summary>
        /// Maps stick to 32 directions. Index 0 = +X, then CCW toward +Z (matches <see cref="DirectionLut"/>).
        /// </summary>
        public static int QuantizeDirection(float axisX, float axisZ)
        {
            double angle = Math.Atan2(axisZ, axisX);
            if (angle < 0.0)
            {
                angle += Math.PI * 2.0;
            }

            int dir = (int)Math.Round(angle / (Math.PI * 2.0) * DirectionLut.Count);
            return dir & (DirectionLut.Count - 1);
        }
    }
}
