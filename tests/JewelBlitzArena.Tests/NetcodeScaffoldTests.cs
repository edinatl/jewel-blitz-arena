using JewelBlitzArena.Net;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class NetcodeScaffoldTests
{
    private const ulong Seed = 0x53375F4E4554UL; // S7_NET

    [Test]
    public void Loopback_zero_latency_stays_in_sync()
    {
        var harness = new DualSessionHarness(inputDelay: 2, deliveryDelayFrames: 0);
        harness.Reset(Seed);

        for (int f = 0; f < 90; f++)
        {
            InputFrame i0 = InputQuantizer.FromCardinal(1, 0);
            InputFrame i1 = InputQuantizer.FromCardinal(-1, 0);
            harness.Tick(i0, i1);
        }

        Assert.That(harness.StatesMatch(), Is.True);
        Assert.That(harness.HashesAgreeAtLastProbe(), Is.True);
        Assert.That(harness.Player0.RollbackCount, Is.EqualTo(0));
        Assert.That(harness.Player1.RollbackCount, Is.EqualTo(0));
        Assert.That(harness.Player0.SimFrame, Is.EqualTo(90));
    }

    [Test]
    public void Loopback_with_delivery_delay_rollbacks_without_desync()
    {
        var harness = new DualSessionHarness(inputDelay: 2, deliveryDelayFrames: 3);
        harness.Reset(Seed);

        for (int f = 0; f < 120; f++)
        {
            // Changing stick each few frames makes repeat-last prediction miss when delayed.
            int dir = (f / 8) % 2 == 0 ? 1 : -1;
            InputFrame i0 = InputQuantizer.FromCardinal(dir, 0, attack: (f % 17) == 0);
            InputFrame i1 = InputQuantizer.FromCardinal(-dir, 0, action: (f % 19) == 0);
            harness.Tick(i0, i1);
        }

        Assert.That(harness.Player0.RollbackCount + harness.Player1.RollbackCount, Is.GreaterThan(0),
            "expected at least one rollback from delayed inputs");
        Assert.That(harness.Player0.PredictionMisses + harness.Player1.PredictionMisses, Is.GreaterThan(0));
        Assert.That(harness.StatesMatch(), Is.True, "peers must converge after rollback");
        Assert.That(harness.HashesAgreeAtLastProbe(), Is.True);
        Assert.That(harness.Player0.DesyncFlagged, Is.False);
        Assert.That(harness.Player1.DesyncFlagged, Is.False);
    }

    [Test]
    public void InputFramePacket_round_trips_bits()
    {
        InputFrame src = InputQuantizer.FromAxes(1f, 0f, jump: true, attack: true);
        InputFramePacket packet = InputFramePacket.From(42, 1, src);
        Assert.That(packet.Frame, Is.EqualTo(42));
        Assert.That(packet.PlayerIndex, Is.EqualTo(1));
        Assert.That(packet.ToInputFrame().Bits, Is.EqualTo(src.Bits));
        Assert.That(InputFrame.SizeOf, Is.EqualTo(2));
    }

    [Test]
    public void SessionConfig_clamps_input_delay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SessionConfig.Lan2P(0, inputDelay: 5));
        SessionConfig ok = SessionConfig.Lan2P(1, inputDelay: 0);
        Assert.That(ok.InputDelay, Is.EqualTo(0));
        Assert.That(ok.RollbackWindow, Is.EqualTo(SessionConfig.DefaultRollbackWindow));
    }

    [Test]
    public void Delayed_local_inputs_match_headless_sim_with_same_log()
    {
        const int delay = 2;
        const int frames = 60;
        ulong seed = Seed ^ 1UL;

        var submitLog = new InputFrame[frames];
        for (int t = 0; t < frames; t++)
        {
            submitLog[t] = InputQuantizer.FromCardinal((t / 10) % 2 == 0 ? 1 : 0, (t / 10) % 2);
        }

        var harness = new DualSessionHarness(inputDelay: delay, deliveryDelayFrames: 0);
        harness.Reset(seed);
        for (int t = 0; t < frames; t++)
        {
            harness.Tick(submitLog[t], InputFrame.Neutral);
        }

        var mirror = new Simulation();
        mirror.Reset(MatchConfig.CreateDefault(seed, 2));
        for (int f = 0; f < frames; f++)
        {
            InputFrame p0 = f < delay ? InputFrame.Neutral : submitLog[f - delay];
            mirror.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(harness.Player0.Sim.Hash(), Is.EqualTo(mirror.Hash()));
        Assert.That(harness.Player1.Sim.Hash(), Is.EqualTo(mirror.Hash()));
    }
}
