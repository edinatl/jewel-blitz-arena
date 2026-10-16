namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Historical name — Expedition now has <see cref="ExpeditionModeOrchestrator"/>.
    /// Kept as a thin alias so older call sites compile.
    /// </summary>
    public sealed class ExpeditionModeStub : ExpeditionModeOrchestrator
    {
    }
}
