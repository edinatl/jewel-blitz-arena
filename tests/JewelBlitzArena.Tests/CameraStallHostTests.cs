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
}
