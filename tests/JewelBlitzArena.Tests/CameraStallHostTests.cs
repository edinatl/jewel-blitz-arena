using JewelBlitzArena.Data;
using JewelBlitzArena.Net;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class CameraStallHostTests
{
    private const ulong Seed = 0x43414D5354414CUL; // CAMSTAL

    [Test]
    public void StageCameraVolumes_resolve_citrine_phase_radii()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetStage(SampleContentFactory.CitrineBazaarStageId, out StageRecord stage), Is.True);
        Assert.That(stage.Phases.Length, Is.GreaterThanOrEqualTo(3));

        var state = new MatchState
        {
            StageId = SampleContentFactory.CitrineBazaarStageId,
            PhaseIndex = 0,
            ActorCount = 2,
        };
        CameraVolume p0 = StageCameraVolumes.Resolve(catalog, in state);
        Assert.That(p0.Radius, Is.EqualTo(12f).Within(0.01f));

        state.PhaseIndex = 1;
        CameraVolume p1 = StageCameraVolumes.Resolve(catalog, in state);
        Assert.That(p1.Radius, Is.EqualTo(14f).Within(0.01f));

        state.PhaseIndex = 2;
        CameraVolume p2 = StageCameraVolumes.Resolve(catalog, in state);
        Assert.That(p2.Radius, Is.EqualTo(16f).Within(0.01f));
    }

    [Test]
    public void StageCameraVolumes_frame_clamps_look_into_volume()
    {
        var volume = new CameraVolume(0f, 8f, 0f, 4f);
        var state = new MatchState { ActorCount = 1 };
        state.Actor0.IsAlive = true;
        state.Actor0.PosX = Fix.FromInt(20);
        state.Actor0.PosZ = Fix.FromInt(0);

        CameraFrame frame = StageCameraVolumes.FrameActors(ref state, in volume, padding: 1f);
        float dx = frame.LookX - volume.CenterX;
        float dz = frame.LookZ - volume.CenterZ;
        float dist = (float)Math.Sqrt((dx * dx) + (dz * dz));
        Assert.That(dist, Is.LessThanOrEqualTo(volume.Radius + 0.01f));
        Assert.That(frame.OrthoSize, Is.LessThanOrEqualTo(volume.Radius + 1f + 0.01f));
        Assert.That(frame.VolumeRadius, Is.EqualTo(4f));
    }

    [Test]
    public void StageCameraVolumes_fit_failure_emits_edge_bubbles()
    {
        var volume = new CameraVolume(0f, 8f, 0f, 4f);
        var state = new MatchState { ActorCount = 2 };
        state.Actor0.IsAlive = true;
        state.Actor0.HeldItemSlot = ActorSlot.NoHeldItem;
        state.Actor0.PosX = Fix.FromInt(-20);
        state.Actor0.PosZ = Fix.FromInt(0);
        state.Actor1.IsAlive = true;
        state.Actor1.HeldItemSlot = ActorSlot.NoHeldItem;
        state.Actor1.PosX = Fix.FromInt(20);
        state.Actor1.PosZ = Fix.FromInt(0);

        CameraFrame frame = StageCameraVolumes.FrameActors(ref state, in volume, padding: 1f);
        Assert.That(frame.FitFailed, Is.True);
        Assert.That(frame.EdgeBubbleCount, Is.EqualTo(2));
        Assert.That(frame.OrthoSize, Is.EqualTo(volume.Radius + 1f).Within(0.01f));

        EdgeBubble a = frame.GetEdgeBubble(0);
        EdgeBubble b = frame.GetEdgeBubble(1);
        Assert.That(a.ActorIndex, Is.EqualTo(0));
        Assert.That(b.ActorIndex, Is.EqualTo(1));
        Assert.That(a.EdgeU, Is.LessThan(0f));
        Assert.That(b.EdgeU, Is.GreaterThan(0f));
        Assert.That(a.JewelsHeld, Is.EqualTo(0));
        Assert.That(a.HasHeldItem, Is.False);
    }

    [Test]
    public void StageCameraVolumes_scripted_path_lerps_during_transition()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var state = new MatchState
        {
            StageId = SampleContentFactory.CitrineBazaarStageId,
            PhaseIndex = 1,
            ActorCount = 2,
            StageFlags = StageFlags.InTransition,
            TransitionRemainingFrames = 60, // half of Breakaway's 120f transition
            PhaseClockPermille = 1000,
        };

        StageCameraVolumes.ResolvePath(
            catalog,
            in state,
            out CameraVolume volume,
            out bool scripted,
            out float pathT);

        Assert.That(scripted, Is.True);
        Assert.That(pathT, Is.EqualTo(0.5f).Within(0.02f));
        // Breakaway authored scripted_camera endRadius=16; start inherits phase0 r=12 → mid ≈ 14
        Assert.That(volume.Radius, Is.EqualTo(14f).Within(0.05f));
        Assert.That(volume.CenterY, Is.EqualTo(9f).Within(0.05f)); // 8 → authored endY 10

        CameraFrame frame = StageCameraVolumes.FrameFromCatalog(catalog, ref state);
        Assert.That(frame.InScriptedPath, Is.True);
        Assert.That(frame.PathT, Is.EqualTo(0.5f).Within(0.02f));
        Assert.That(frame.VolumeRadius, Is.EqualTo(14f).Within(0.05f));

        Assert.That(catalog.TryGetStage(SampleContentFactory.CitrineBazaarStageId, out StageRecord stage), Is.True);
        Assert.That(stage.Phases[1].HasScriptedCamera, Is.True);
        Assert.That((stage.Phases[1].ScriptedCameraMask & ScriptedCameraMaskBits.EndR) != 0, Is.True);
    }

    [Test]
    public void RollbackSession_stalls_when_remote_silent_past_window()
    {
        var hub = new LoopbackHub { DeliveryDelayFrames = 0 };
        // Peer 0 alone — peer 1 never advances, so remote inputs never arrive.
        INetTransport t0 = hub.CreateEndpoint(0, new[] { 1 });
        var session = new RollbackSession(SessionConfig.Lan2P(0, inputDelay: 0), t0);
        session.Reset(MatchConfig.CreateDefault(Seed, 2));

        int stepped = 0;
        int stalled = 0;
        for (int i = 0; i < SessionConfig.DefaultRollbackWindow + 5; i++)
        {
            hub.TickClock();
            if (session.Advance(InputQuantizer.FromCardinal(1, 0)))
            {
                stepped++;
            }
            else
            {
                stalled++;
            }
        }

        Assert.That(stepped, Is.EqualTo(SessionConfig.DefaultRollbackWindow + 1));
        Assert.That(stalled, Is.GreaterThan(0));
        Assert.That(session.IsStalling, Is.True);
        Assert.That(session.StallFrames, Is.GreaterThan(0));
        Assert.That(session.StalledRemotePlayer, Is.EqualTo(1));
    }

    [Test]
    public void RollbackSession_clears_stall_when_remote_catches_up()
    {
        var harness = new DualSessionHarness(inputDelay: 0, deliveryDelayFrames: 0);
        harness.Reset(Seed);

        // Run normally — should not stall.
        for (int f = 0; f < 40; f++)
        {
            harness.Tick(InputQuantizer.FromCardinal(1, 0), InputQuantizer.FromCardinal(-1, 0));
        }

        Assert.That(harness.Player0.IsStalling, Is.False);
        Assert.That(harness.Player1.IsStalling, Is.False);
        Assert.That(harness.StatesMatch(), Is.True);
    }

    [Test]
    public void SessionConfig_stall_thresholds_match_architecture()
    {
        Assert.That(SessionConfig.StallDisconnectWarnFrames, Is.EqualTo(180));
        Assert.That(SessionConfig.StallDisconnectReplaceFrames, Is.EqualTo(600));
    }

    [Test]
    public void RollbackSession_replaces_stalled_peer_with_cpu_after_threshold()
    {
        var hub = new LoopbackHub { DeliveryDelayFrames = 0 };
        INetTransport t0 = hub.CreateEndpoint(0, new[] { 1 });
        var session = new RollbackSession(SessionConfig.Lan2P(0, inputDelay: 0), t0);
        session.Reset(MatchConfig.CreateDefault(Seed, 2));

        int frameBeforeStall = session.SimFrame;
        // Predict through the window, then stall until replace threshold.
        int budget = SessionConfig.DefaultRollbackWindow
            + SessionConfig.StallDisconnectReplaceFrames
            + 8;
        bool replaced = false;
        for (int i = 0; i < budget; i++)
        {
            hub.TickClock();
            bool advanced = session.Advance(InputQuantizer.FromCardinal(1, 0));
            if (session.IsCpuReplaced(1))
            {
                replaced = true;
                Assert.That(advanced, Is.True, "match should step again once CPU fills the peer");
                break;
            }
        }

        Assert.That(replaced, Is.True);
        Assert.That(session.IsCpuReplaced(1), Is.True);
        Assert.That(session.Sim.IsCpuControlled(1), Is.True);
        Assert.That(session.IsStalling, Is.False);
        Assert.That(session.SimFrame, Is.GreaterThan(frameBeforeStall + SessionConfig.DefaultRollbackWindow));

        // CPU keeps the match deterministic across continued steps.
        ulong h0 = session.Sim.Hash();
        for (int i = 0; i < 30; i++)
        {
            hub.TickClock();
            Assert.That(session.Advance(InputQuantizer.FromCardinal(1, 0)), Is.True);
        }

        Assert.That(session.Sim.Hash(), Is.Not.EqualTo(h0));
        Assert.That(session.Sim.IsCpuControlled(1), Is.True);
    }

    [Test]
    public void CpuBrain_chase_is_hash_stable()
    {
        MatchConfig config = MatchConfig.CreateDefault(Seed ^ 0xC0FFEEUL, actorCount: 2);
        var a = new Simulation();
        var b = new Simulation();
        a.Reset(config);
        b.Reset(config);
        a.SetCpuControlled(1, true);
        b.SetCpuControlled(1, true);

        InputFrame p0 = InputQuantizer.FromCardinal(1, 0);
        for (int i = 0; i < 90; i++)
        {
            a.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            b.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(a.Hash(), Is.EqualTo(b.Hash()));
        Assert.That(a.IsCpuControlled(1), Is.True);
        MatchState snap = a.Snapshot();
        Assert.That(snap.Actor1.PosX.Raw, Is.Not.EqualTo(Fix.FromInt(-1).Raw));
    }
}
