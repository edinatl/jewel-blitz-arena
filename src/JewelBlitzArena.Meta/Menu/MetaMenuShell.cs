using System.Collections.Generic;

namespace JewelBlitzArena.Meta
{
    /// <summary>Skeletal main-menu entries. Presentation binds labels; Meta owns availability.</summary>
    public sealed class MetaMenuShell
    {
        private readonly TinkerModeStub _tinker = new TinkerModeStub();

        public IReadOnlyList<MetaMenuEntry> BuildEntries(PlayerProfile profile)
        {
            return new[]
            {
                new MetaMenuEntry(ModeId.Custom, "Custom", available: true),
                new MetaMenuEntry(ModeId.Sparring, "Sparring", available: true),
                new MetaMenuEntry(ModeId.Duel, "Duel", available: false, reason: "M2"),
                new MetaMenuEntry(ModeId.Tournament, "Tournament", available: false, reason: "M2"),
                new MetaMenuEntry(ModeId.Expedition, "Expedition", available: false, reason: "M2 stub"),
                new MetaMenuEntry(ModeId.TillysBench, _tinker.MenuLabel, available: true),
                new MetaMenuEntry(ModeId.Online, "Online", available: false, reason: "S7"),
            };
        }

        public void OpenTinker(PlayerProfile profile) => _tinker.Open(profile);
    }

    public readonly struct MetaMenuEntry
    {
        public MetaMenuEntry(ModeId mode, string label, bool available, string? reason = null)
        {
            Mode = mode;
            Label = label;
            Available = available;
            UnavailableReason = reason;
        }

        public ModeId Mode { get; }
        public string Label { get; }
        public bool Available { get; }
        public string? UnavailableReason { get; }
    }
}
