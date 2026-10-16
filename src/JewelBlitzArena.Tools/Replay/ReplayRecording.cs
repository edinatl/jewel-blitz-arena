using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tools.Replay;

/// <summary>
/// Versioned replay: <see cref="MatchConfig"/> + per-frame inputs + expected terminal hash.
/// Format version bumps when the wire layout changes.
/// </summary>
public sealed class ReplayRecording
{
    /// <summary>v2: CPU handoffs. v3: optional item spawn handoffs for status-patch corpus.</summary>
    public const int CurrentFormatVersion = 3;
    public const int MinSupportedFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;

    public string Name { get; set; } = "";

    public string? Notes { get; set; }

    public ulong Seed { get; set; }

    public int StageId { get; set; }

    public int ActorCount { get; set; }

    /// <summary>
    /// House Rule 7 Phase Clock as permille. 0 = default 1000 (100%).
    /// Additive field — older fixtures omit it and keep full-speed clocks.
    /// </summary>
    public int PhaseClockPermille { get; set; }

    public int[] FighterIds { get; set; } = new int[PoolCaps.Actors];

    /// <summary>Expected xxHash64 after replaying every frame (hex string, e.g. "a1b2c3d4e5f60718").</summary>
    public string ExpectedHashHex { get; set; } = "";

    /// <summary>Packed <see cref="InputFrame.Bits"/> for slots 0..3 each frame.</summary>
    public FrameInputs[] Frames { get; set; } = Array.Empty<FrameInputs>();

    /// <summary>
    /// CPU handoffs applied before stepping the given frame index (format v2).
    /// Sim overwrites that slot's input from the handoff frame onward.
    /// </summary>
    public CpuHandoff[] CpuHandoffs { get; set; } = Array.Empty<CpuHandoff>();

    /// <summary>
    /// Loose-item spawns applied before stepping the given frame index (format v3).
    /// Used to seed Ice/Smoke/Adhesive jars for world-patch determinism corpus.
    /// </summary>
    public ItemSpawn[] ItemSpawns { get; set; } = Array.Empty<ItemSpawn>();

    public sealed class FrameInputs
    {
        public ushort P0 { get; set; }
        public ushort P1 { get; set; }
        public ushort P2 { get; set; }
        public ushort P3 { get; set; }
    }

    public sealed class CpuHandoff
    {
        /// <summary>0-based frame index in <see cref="Frames"/> at which CPU takes over (before Step).</summary>
        public int Frame { get; set; }

        public int PlayerIndex { get; set; }
    }

    public sealed class ItemSpawn
    {
        /// <summary>0-based frame index in <see cref="Frames"/> (before Step).</summary>
        public int Frame { get; set; }

        public int ItemId { get; set; }

        /// <summary>Integer metres (converted to Fix at spawn).</summary>
        public int X { get; set; }

        public int Y { get; set; }

        public int Z { get; set; }

        /// <summary>
        /// When true, also arms the Ice/Smoke/Adhesive world patch at spawn
        /// (status-item corpus; avoids depending on flight settle timing).
        /// </summary>
        public bool SpawnWorldPatch { get; set; }
    }

    public MatchConfig ToMatchConfig()
    {
        var config = MatchConfig.CreateDefault(Seed, ActorCount);
        config.StageId = StageId;
        if (PhaseClockPermille > 0)
        {
            config.PhaseClockPermille = PhaseClockPermille;
        }

        for (int i = 0; i < PoolCaps.Actors; i++)
        {
            int id = i < FighterIds.Length ? FighterIds[i] : i;
            config.SetFighterId(i, id);
        }

        return config;
    }

    public static ulong ParseHashHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            throw new FormatException("ExpectedHashHex is empty.");
        }

        string trimmed = hex.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[2..];
        }

        return Convert.ToUInt64(trimmed, 16);
    }

    public static string FormatHashHex(ulong hash) => hash.ToString("x16");
}
