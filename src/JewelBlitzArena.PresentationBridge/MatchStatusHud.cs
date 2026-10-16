using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-only status / immunity HUD hints (balance.md §4 immunity halo).
    /// Not hashed — read from live <see cref="ActorSlot"/> timers.
    /// </summary>
    public readonly struct MatchStatusHud
    {
        public ushort StunFrames { get; }
        public ushort BlindFrames { get; }
        public ushort DizzyFrames { get; }
        public ushort SlowFrames { get; }
        public ushort PinFrames { get; }
        public ushort FreezeFrames { get; }
        public ushort DizzyImmuneFrames { get; }
        public ushort PinImmuneFrames { get; }
        public ushort FreezeImmuneFrames { get; }

        public bool HasActiveStatus =>
            StunFrames > 0
            || BlindFrames > 0
            || DizzyFrames > 0
            || SlowFrames > 0
            || PinFrames > 0
            || FreezeFrames > 0;

        /// <summary>True when any post-control immunity is active (HUD halo).</summary>
        public bool HasImmunityHalo =>
            DizzyImmuneFrames > 0 || PinImmuneFrames > 0 || FreezeImmuneFrames > 0;

        public MatchStatusHud(
            ushort stun,
            ushort blind,
            ushort dizzy,
            ushort slow,
            ushort pin,
            ushort freeze,
            ushort dizzyImmune,
            ushort pinImmune,
            ushort freezeImmune)
        {
            StunFrames = stun;
            BlindFrames = blind;
            DizzyFrames = dizzy;
            SlowFrames = slow;
            PinFrames = pin;
            FreezeFrames = freeze;
            DizzyImmuneFrames = dizzyImmune;
            PinImmuneFrames = pinImmune;
            FreezeImmuneFrames = freezeImmune;
        }

        public static MatchStatusHud FromActor(in ActorSlot actor) =>
            new MatchStatusHud(
                stun: actor.StunFrames > 0 ? (ushort)System.Math.Min(actor.StunFrames, ushort.MaxValue) : (ushort)0,
                blind: actor.BlindFrames,
                dizzy: actor.DizzyFrames,
                slow: actor.SlowFrames,
                pin: actor.PinFrames,
                freeze: actor.FreezeFrames,
                dizzyImmune: actor.DizzyImmuneFrames,
                pinImmune: actor.PinImmuneFrames,
                freezeImmune: actor.FreezeImmuneFrames);

        /// <summary>Compact active-status tags, e.g. <c>SLW 119 FRZ 90</c>.</summary>
        public string FormatActive()
        {
            // Avoid alloc-heavy StringBuilder for greybox OnGUI; build via small concatenations.
            string s = string.Empty;
            if (StunFrames > 0)
            {
                s += (s.Length > 0 ? " " : string.Empty) + "STN " + StunFrames;
            }

            if (BlindFrames > 0)
            {
                s += (s.Length > 0 ? " " : string.Empty) + "BLD " + BlindFrames;
            }

            if (DizzyFrames > 0)
            {
                s += (s.Length > 0 ? " " : string.Empty) + "DZY " + DizzyFrames;
            }

            if (SlowFrames > 0)
            {
                s += (s.Length > 0 ? " " : string.Empty) + "SLW " + SlowFrames;
            }

            if (PinFrames > 0)
            {
                s += (s.Length > 0 ? " " : string.Empty) + "PIN " + PinFrames;
            }

            if (FreezeFrames > 0)
            {
                s += (s.Length > 0 ? " " : string.Empty) + "FRZ " + FreezeFrames;
            }

            return s;
        }

        /// <summary>Immunity halo tags, e.g. <c>IMM DZY PIN</c>.</summary>
        public string FormatImmunity()
        {
            if (!HasImmunityHalo)
            {
                return string.Empty;
            }

            string s = "IMM";
            if (DizzyImmuneFrames > 0)
            {
                s += " DZY";
            }

            if (PinImmuneFrames > 0)
            {
                s += " PIN";
            }

            if (FreezeImmuneFrames > 0)
            {
                s += " FRZ";
            }

            return s;
        }
    }
}
