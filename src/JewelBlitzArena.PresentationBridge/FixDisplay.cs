using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Display-only Fix → float conversion. Never feed results back into Sim.
    /// </summary>
    public static class FixDisplay
    {
        public static float ToFloat(Fix value) => (float)((double)value.Raw / Fix.OneRaw);

        public static double ToDouble(Fix value) => (double)value.Raw / Fix.OneRaw;
    }
}
