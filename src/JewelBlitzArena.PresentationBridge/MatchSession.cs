using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Presentation-owned driver around <see cref="Simulation"/>.
    /// Keeps previous/current snapshots for interpolation; never mutates rules.
    /// </summary>
    public sealed class MatchSession
    {
        private readonly Simulation _sim = new Simulation();
        private MatchState _previous;
        private MatchState _current;
        private ulong _seed;
        private int _actorCount;
        private ContentCatalog? _catalog;

        public Simulation Sim => _sim;

        public ref readonly MatchState Previous => ref _previous;

        public ref readonly MatchState Current => ref _current;

        public int Frame => _sim.Frame;

        public ulong Seed => _seed;

        public int ActorCount => _actorCount;

        public bool MatchOver => _sim.MatchOver;

        public void Reset(in MatchConfig config, ContentCatalog? catalog = null)
        {
            _seed = config.Seed;
            _actorCount = config.ActorCount;
            _catalog = catalog;
            _sim.Reset(config, catalog);
            _current = _sim.Snapshot();
            _previous = _current;
        }

        public void ResetDefault(ulong seed, int actorCount = 2, ContentCatalog? catalog = null)
        {
            MatchConfig config = MatchConfig.CreateDefault(seed, actorCount);
            Reset(config, catalog);
        }

        public void Step(InputFrame p0, InputFrame p1, InputFrame p2 = default, InputFrame p3 = default)
        {
            _previous = _current;
            _sim.Step(p0, p1, p2, p3);
            _current = _sim.Snapshot();
        }

        public void Step(InputFrame[] inputs)
        {
            InputFrame i0 = inputs.Length > 0 ? inputs[0] : InputFrame.Neutral;
            InputFrame i1 = inputs.Length > 1 ? inputs[1] : InputFrame.Neutral;
            InputFrame i2 = inputs.Length > 2 ? inputs[2] : InputFrame.Neutral;
            InputFrame i3 = inputs.Length > 3 ? inputs[3] : InputFrame.Neutral;
            Step(i0, i1, i2, i3);
        }

        public ulong Hash() => _sim.Hash();

        /// <summary>
        /// Restart from the same seed and prove presentation restart matches a prior hash.
        /// </summary>
        public ulong RestartAndHashNeutral(int frames)
        {
            MatchConfig config = MatchConfig.CreateDefault(_seed, _actorCount);
            Reset(config, _catalog);
            for (int i = 0; i < frames; i++)
            {
                Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            }

            return Hash();
        }
    }
}
