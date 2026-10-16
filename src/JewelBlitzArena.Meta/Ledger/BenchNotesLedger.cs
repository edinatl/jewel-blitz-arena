using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Meta
{
    /// <summary>
    /// Bench Notes + Logbook pool reveals (items-and-economy.md §4).
    /// Pair keys are order-independent (min,max item ids).
    /// </summary>
    public sealed class BenchNotesLedger
    {
        public const byte OutcomeFailure = 1 << 0;
        public const byte OutcomeSuccess = 1 << 1;
        public const byte OutcomeDouble = 1 << 2;

        public readonly struct Note
        {
            public Note(int inputA, int inputB, byte outcomesSeen, bool poolRevealed)
            {
                InputA = inputA;
                InputB = inputB;
                OutcomesSeen = outcomesSeen;
                PoolRevealed = poolRevealed;
            }

            public int InputA { get; }
            public int InputB { get; }
            public byte OutcomesSeen { get; }
            public bool PoolRevealed { get; }
        }

        private readonly Dictionary<long, Entry> _notes = new Dictionary<long, Entry>();

        private struct Entry
        {
            public byte OutcomesSeen;
            public bool PoolRevealed;
        }

        public int Count => _notes.Count;

        public static long PairKey(int a, int b)
        {
            int lo = a < b ? a : b;
            int hi = a < b ? b : a;
            return ((long)(uint)lo << 32) | (uint)hi;
        }

        public static byte OutcomeBit(TinkerOutcome outcome)
        {
            switch (outcome)
            {
                case TinkerOutcome.Success: return OutcomeSuccess;
                case TinkerOutcome.Double: return OutcomeDouble;
                default: return OutcomeFailure;
            }
        }

        public void RecordAttempt(int inputA, int inputB, TinkerOutcome outcome)
        {
            long key = PairKey(inputA, inputB);
            _notes.TryGetValue(key, out Entry e);
            e.OutcomesSeen |= OutcomeBit(outcome);
            _notes[key] = e;
        }

        public bool HasSeen(int inputA, int inputB, TinkerOutcome outcome)
        {
            long key = PairKey(inputA, inputB);
            return _notes.TryGetValue(key, out Entry e)
                && (e.OutcomesSeen & OutcomeBit(outcome)) != 0;
        }

        public bool IsPoolRevealed(int inputA, int inputB)
        {
            long key = PairKey(inputA, inputB);
            return _notes.TryGetValue(key, out Entry e) && e.PoolRevealed;
        }

        /// <summary>Logbook gift: unlock full pool note for the pair (not the roll).</summary>
        public bool RevealPool(int inputA, int inputB)
        {
            long key = PairKey(inputA, inputB);
            _notes.TryGetValue(key, out Entry e);
            if (e.PoolRevealed)
            {
                return false;
            }

            e.PoolRevealed = true;
            _notes[key] = e;
            return true;
        }

        public IReadOnlyList<Note> ToList()
        {
            var list = new List<Note>(_notes.Count);
            foreach (KeyValuePair<long, Entry> kv in _notes)
            {
                int a = (int)(kv.Key >> 32);
                int b = (int)(uint)kv.Key;
                list.Add(new Note(a, b, kv.Value.OutcomesSeen, kv.Value.PoolRevealed));
            }

            list.Sort((x, y) =>
            {
                int c = x.InputA.CompareTo(y.InputA);
                return c != 0 ? c : x.InputB.CompareTo(y.InputB);
            });
            return list;
        }

        public void ReplaceAll(IEnumerable<Note> notes)
        {
            _notes.Clear();
            if (notes == null)
            {
                return;
            }

            foreach (Note n in notes)
            {
                _notes[PairKey(n.InputA, n.InputB)] = new Entry
                {
                    OutcomesSeen = n.OutcomesSeen,
                    PoolRevealed = n.PoolRevealed,
                };
            }
        }

        public void Clear() => _notes.Clear();
    }
}
