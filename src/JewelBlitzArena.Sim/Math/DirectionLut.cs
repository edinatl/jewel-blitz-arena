namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// 32 unit directions for quantized stick input (architecture §3).
    /// Raw Q48.16 constants were authored offline; no runtime float conversion.
    /// </summary>
    public static class DirectionLut
    {
        public const int Count = 32;

        private static readonly FixVec2[] Unit =
        {
            new FixVec2(new Fix(65536), new Fix(0)),
            new FixVec2(new Fix(64277), new Fix(12785)),
            new FixVec2(new Fix(60547), new Fix(25080)),
            new FixVec2(new Fix(54491), new Fix(36410)),
            new FixVec2(new Fix(46341), new Fix(46341)),
            new FixVec2(new Fix(36410), new Fix(54491)),
            new FixVec2(new Fix(25080), new Fix(60547)),
            new FixVec2(new Fix(12785), new Fix(64277)),
            new FixVec2(new Fix(0), new Fix(65536)),
            new FixVec2(new Fix(-12785), new Fix(64277)),
            new FixVec2(new Fix(-25080), new Fix(60547)),
            new FixVec2(new Fix(-36410), new Fix(54491)),
            new FixVec2(new Fix(-46341), new Fix(46341)),
            new FixVec2(new Fix(-54491), new Fix(36410)),
            new FixVec2(new Fix(-60547), new Fix(25080)),
            new FixVec2(new Fix(-64277), new Fix(12785)),
            new FixVec2(new Fix(-65536), new Fix(0)),
            new FixVec2(new Fix(-64277), new Fix(-12785)),
            new FixVec2(new Fix(-60547), new Fix(-25080)),
            new FixVec2(new Fix(-54491), new Fix(-36410)),
            new FixVec2(new Fix(-46341), new Fix(-46341)),
            new FixVec2(new Fix(-36410), new Fix(-54491)),
            new FixVec2(new Fix(-25080), new Fix(-60547)),
            new FixVec2(new Fix(-12785), new Fix(-64277)),
            new FixVec2(new Fix(0), new Fix(-65536)),
            new FixVec2(new Fix(12785), new Fix(-64277)),
            new FixVec2(new Fix(25080), new Fix(-60547)),
            new FixVec2(new Fix(36410), new Fix(-54491)),
            new FixVec2(new Fix(46341), new Fix(-46341)),
            new FixVec2(new Fix(54491), new Fix(-36410)),
            new FixVec2(new Fix(60547), new Fix(-25080)),
            new FixVec2(new Fix(64277), new Fix(-12785)),
        };

        /// <summary>Unit vector for direction index 0..31.</summary>
        public static FixVec2 GetUnit(int directionIndex)
        {
            int i = directionIndex & (Count - 1);
            return Unit[i];
        }
    }
}
