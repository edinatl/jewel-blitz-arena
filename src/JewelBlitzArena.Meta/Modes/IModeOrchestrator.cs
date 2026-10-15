using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Mode shell: build <see cref="MatchConfig"/> from profile + lobby, then ingest <see cref="MatchResult"/>.
    /// Never mutates <see cref="MatchState"/>.
    /// </summary>
    public interface IModeOrchestrator
    {
        ModeId Mode { get; }

        /// <summary>True when ApplyResult may write profile / Ledger.</summary>
        bool WritesProgression { get; }

        MatchConfig BuildConfig(PlayerProfile profile, ModeRequest request);

        void ApplyResult(PlayerProfile profile, in MatchResult result, ModeRequest request);
    }
}
