using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Net
{
    /// <summary>
    /// Two local <see cref="RollbackSession"/> peers over <see cref="LoopbackHub"/>.
    /// Used for headless rollback / desync tests (S7 exit path without real sockets).
    /// </summary>
    public sealed class DualSessionHarness
    {
        private readonly LoopbackHub _hub;
        private readonly RollbackSession _p0;
        private readonly RollbackSession _p1;

        public DualSessionHarness(int inputDelay = SessionConfig.DefaultInputDelay, int deliveryDelayFrames = 0)
        {
            _hub = new LoopbackHub { DeliveryDelayFrames = deliveryDelayFrames };
            INetTransport t0 = _hub.CreateEndpoint(0, new[] { 1 });
            INetTransport t1 = _hub.CreateEndpoint(1, new[] { 0 });
            _p0 = new RollbackSession(SessionConfig.Lan2P(0, inputDelay), t0);
            _p1 = new RollbackSession(SessionConfig.Lan2P(1, inputDelay), t1);
        }

        public LoopbackHub Hub => _hub;

        public RollbackSession Player0 => _p0;

        public RollbackSession Player1 => _p1;

        public void Reset(ulong seed, ContentCatalog? catalog = null)
        {
            _hub.ResetClock();
            MatchConfig config = MatchConfig.CreateDefault(seed, actorCount: 2);
            _p0.Reset(config, catalog);
            _p1.Reset(config, catalog);
        }

        /// <summary>
        /// One lockstep tick: advance hub clock, then both peers with their local inputs.
        /// </summary>
        public void Tick(InputFrame inputP0, InputFrame inputP1)
        {
            _hub.TickClock();
            _p0.Advance(inputP0);
            _p1.Advance(inputP1);
        }

        public bool HashesAgreeAtLastProbe()
        {
            if (_p0.LastHashFrame < 0 || _p0.LastHashFrame != _p1.LastHashFrame)
            {
                return true;
            }

            bool ok0 = _p0.TryCompareHash(_p1.LastHashFrame, _p1.LastHash);
            bool ok1 = _p1.TryCompareHash(_p0.LastHashFrame, _p0.LastHash);
            return ok0 && ok1 && !_p0.DesyncFlagged && !_p1.DesyncFlagged;
        }

        public bool StatesMatch() => _p0.Sim.Hash() == _p1.Sim.Hash();
    }
}
