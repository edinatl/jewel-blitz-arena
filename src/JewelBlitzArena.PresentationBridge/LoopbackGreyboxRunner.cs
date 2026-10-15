using JewelBlitzArena.Data;
using JewelBlitzArena.Net;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.PresentationBridge
{
    /// <summary>
    /// Headless / Unity-shared 2P greybox over <see cref="DualSessionHarness"/> (loopback rollback).
    /// Presentation feeds quantized inputs; Sim remains the sole state mutator.
    /// </summary>
    public sealed class LoopbackGreyboxRunner
    {
        private readonly DualSessionHarness _harness;
        private MatchState _previous;
        private MatchState _current;

        public LoopbackGreyboxRunner(int inputDelay = SessionConfig.DefaultInputDelay, int deliveryDelayFrames = 0)
        {
            _harness = new DualSessionHarness(inputDelay, deliveryDelayFrames);
        }

        public DualSessionHarness Harness => _harness;

        public ref readonly MatchState Previous => ref _previous;

        public ref readonly MatchState Current => ref _current;

        public int Frame => _harness.Player0.SimFrame;

        public ulong Hash => _harness.Player0.Sim.Hash();

        public int RollbackCount =>
            _harness.Player0.RollbackCount + _harness.Player1.RollbackCount;

        public bool DesyncFlagged =>
            _harness.Player0.DesyncFlagged || _harness.Player1.DesyncFlagged;

        public void Reset(ulong seed, ContentCatalog? catalog = null)
        {
            _harness.Reset(seed, catalog);
            _current = _harness.Player0.Sim.Snapshot();
            _previous = _current;
        }

        public void Reset(in MatchConfig config, ContentCatalog? catalog = null)
        {
            _harness.Hub.ResetClock();
            _harness.Player0.Reset(config, catalog);
            _harness.Player1.Reset(config, catalog);
            _current = _harness.Player0.Sim.Snapshot();
            _previous = _current;
        }

        public void Tick(InputFrame inputP0, InputFrame inputP1)
        {
            _previous = _current;
            _harness.Tick(inputP0, inputP1);
            _current = _harness.Player0.Sim.Snapshot();
        }

        public bool StatesMatch() => _harness.StatesMatch();

        public bool HashesAgree() => _harness.HashesAgreeAtLastProbe();
    }
}
