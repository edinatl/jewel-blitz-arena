using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Net
{
    /// <summary>Circular buffer of sim snapshots for rollback restores.</summary>
    public sealed class SnapshotRing
    {
        private readonly MatchState[] _states;
        private readonly int[] _frames;
        private readonly bool[] _valid;
        private readonly int _capacity;

        public SnapshotRing(int capacity)
        {
            _capacity = capacity;
            _states = new MatchState[capacity];
            _frames = new int[capacity];
            _valid = new bool[capacity];
        }

        public int Capacity => _capacity;

        public void Reset()
        {
            for (int i = 0; i < _capacity; i++)
            {
                _valid[i] = false;
                _frames[i] = -1;
                _states[i] = default;
            }
        }

        /// <summary>Store snapshot taken after completing <paramref name="frameCompleted"/> (Sim.Frame).</summary>
        public void Store(int frameCompleted, in MatchState state)
        {
            int idx = Mod(frameCompleted);
            _frames[idx] = frameCompleted;
            _states[idx] = state;
            _valid[idx] = true;
        }

        public bool TryLoad(int frameCompleted, out MatchState state)
        {
            int idx = Mod(frameCompleted);
            if (!_valid[idx] || _frames[idx] != frameCompleted)
            {
                state = default;
                return false;
            }

            state = _states[idx];
            return true;
        }

        private int Mod(int frame)
        {
            int m = frame % _capacity;
            return m < 0 ? m + _capacity : m;
        }
    }
}
