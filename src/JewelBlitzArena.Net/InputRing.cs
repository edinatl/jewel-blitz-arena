using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// Per-player input buffer with confirmed vs predicted distinction.
    /// Slots are keyed by <c>frame % capacity</c> and tagged with the absolute frame
    /// so <see cref="AdvanceBase"/> cannot remap another frame onto the same slot.
    /// Prediction: repeat last confirmed input for a frame strictly before the request.
    /// </summary>
    public sealed class InputRing
    {
        private readonly int _capacity;
        private readonly InputFrame[] _frames;
        private readonly int[] _frameTags;
        private readonly byte[] _flags; // 0 empty, 1 predicted, 2 confirmed
        private int _baseFrame;

        private const byte Empty = 0;
        private const byte Predicted = 1;
        private const byte Confirmed = 2;

        public InputRing(int capacity)
        {
            _capacity = capacity;
            _frames = new InputFrame[capacity];
            _frameTags = new int[capacity];
            _flags = new byte[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _frameTags[i] = -1;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _capacity; i++)
            {
                _frames[i] = InputFrame.Neutral;
                _flags[i] = Empty;
                _frameTags[i] = -1;
            }

            _baseFrame = 0;
        }

        public void AdvanceBase(int newBaseFrame)
        {
            if (newBaseFrame <= _baseFrame)
            {
                return;
            }

            for (int f = _baseFrame; f < newBaseFrame; f++)
            {
                ClearFrame(f);
            }

            _baseFrame = newBaseFrame;
        }

        public bool IsConfirmed(int frame)
        {
            int idx = IndexOf(frame);
            return _flags[idx] == Confirmed && _frameTags[idx] == frame;
        }

        public InputFrame GetOrPredict(int frame)
        {
            int idx = IndexOf(frame);
            if (_frameTags[idx] == frame && (_flags[idx] == Confirmed || _flags[idx] == Predicted))
            {
                return _frames[idx];
            }

            InputFrame predicted = FindLastConfirmedBefore(frame);
            _frames[idx] = predicted;
            _flags[idx] = Predicted;
            _frameTags[idx] = frame;
            return predicted;
        }

        /// <summary>
        /// Returns true if the confirmed value differs from what was previously stored (prediction miss).
        /// </summary>
        public bool Confirm(int frame, InputFrame input)
        {
            int idx = IndexOf(frame);
            bool had = _frameTags[idx] == frame && _flags[idx] != Empty;
            bool mismatch = had && _frames[idx].Bits != input.Bits;
            _frames[idx] = input;
            _flags[idx] = Confirmed;
            _frameTags[idx] = frame;
            if (mismatch)
            {
                InvalidatePredictionsAfter(frame);
            }

            return mismatch;
        }

        public void SetLocalScheduled(int frame, InputFrame input) => Confirm(frame, input);

        public void InvalidatePredictionsAfter(int frame)
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (_flags[i] == Predicted && _frameTags[i] > frame)
                {
                    _flags[i] = Empty;
                    _frameTags[i] = -1;
                    _frames[i] = InputFrame.Neutral;
                }
            }
        }

        private InputFrame FindLastConfirmedBefore(int frame)
        {
            int bestFrame = -1;
            InputFrame best = InputFrame.Neutral;
            for (int i = 0; i < _capacity; i++)
            {
                if (_flags[i] != Confirmed)
                {
                    continue;
                }

                int f = _frameTags[i];
                if (f >= 0 && f < frame && f >= bestFrame)
                {
                    bestFrame = f;
                    best = _frames[i];
                }
            }

            return best;
        }

        private void ClearFrame(int frame)
        {
            int idx = IndexOf(frame);
            if (_frameTags[idx] == frame)
            {
                _flags[idx] = Empty;
                _frameTags[idx] = -1;
                _frames[idx] = InputFrame.Neutral;
            }
        }

        private int IndexOf(int frame)
        {
            int m = frame % _capacity;
            return m < 0 ? m + _capacity : m;
        }
    }
}
