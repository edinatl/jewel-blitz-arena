using JewelBlitzArena.Net;
using JewelBlitzArena.PresentationBridge;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class CpuHandoffAgreeTests
{
    private const ulong Seed = 0x48414E444F4646UL; // HANDOFF

    [Test]
    public void Dual_remaining_peers_agree_frame_for_cpu_handoff()
    {
        // 3P lobby: peers 0 and 1 remain and must agree; peer 2 is the replace target.
        var hub = new LoopbackHub { DeliveryDelayFrames = 0 };
        var s0 = new RollbackSession(
            new SessionConfig(localPlayerIndex: 0, playerCount: 3, inputDelay: 0),
            hub.CreateEndpoint(0, new[] { 1, 2 }));
        var s1 = new RollbackSession(
            new SessionConfig(localPlayerIndex: 1, playerCount: 3, inputDelay: 0),
            hub.CreateEndpoint(1, new[] { 0, 2 }));

        MatchConfig config = MatchConfig.CreateDefault(Seed, actorCount: 3);
        s0.Reset(config);
        s1.Reset(config);

        // Lockstep without peer 2 (already "gone") — predict through the window then stall.
        // For agree-frame we propose explicitly before the 10s timer; inject votes directly.
        const int warm = 16;
        for (int f = 0; f < warm; f++)
        {
            hub.TickClock();
            // Feed synthetic remote inputs so neither stalls during warmup.
            s0.InjectRemote(InputFramePacket.From(f, 1, InputQuantizer.FromCardinal(-1, 0)));
            s0.InjectRemote(InputFramePacket.From(f, 2, InputQuantizer.FromCardinal(0, 1)));
            s1.InjectRemote(InputFramePacket.From(f, 0, InputQuantizer.FromCardinal(1, 0)));
            s1.InjectRemote(InputFramePacket.From(f, 2, InputQuantizer.FromCardinal(0, 1)));
            Assert.That(s0.Advance(InputQuantizer.FromCardinal(1, 0)), Is.True);
            Assert.That(s1.Advance(InputQuantizer.FromCardinal(-1, 0)), Is.True);
        }

        int agreeFrame = s0.SimFrame;
        Assert.That(s1.SimFrame, Is.EqualTo(agreeFrame));

        // Peer 0 proposes; peer 1 has not voted yet → no commit.
        Assert.That(s0.ProposeCpuHandoff(targetPlayer: 2, agreeFrame: agreeFrame), Is.False);
        Assert.That(s0.PendingHandoffTarget, Is.EqualTo(2));
        Assert.That(s0.HandoffVoteMask & 0x1, Is.Not.EqualTo(0));

        // Exchange votes explicitly (deterministic; no reliance on stall auto-ack).
        s1.InjectRemote(InputFramePacket.CpuHandoffPropose(agreeFrame, targetPlayer: 2, proposer: 0));
        Assert.That(s1.ProposeCpuHandoff(targetPlayer: 2, agreeFrame: agreeFrame), Is.True,
            "peer 1's vote completes the remaining-peer quorum");
        Assert.That(s1.IsCpuReplaced(2), Is.True);

        s0.InjectRemote(InputFramePacket.CpuHandoffPropose(agreeFrame, targetPlayer: 2, proposer: 1));
        // Inject of peer 1's vote may commit immediately; otherwise re-propose to finish quorum.
        if (!s0.IsCpuReplaced(2))
        {
            Assert.That(s0.ProposeCpuHandoff(targetPlayer: 2, agreeFrame: agreeFrame), Is.True);
        }

        Assert.That(s0.IsCpuReplaced(2), Is.True);
        Assert.That(s0.Sim.IsCpuControlled(2), Is.True);
        Assert.That(s1.Sim.IsCpuControlled(2), Is.True);

        // Continue without peer 2 — hashes stay locked (CPU brain is deterministic).
        for (int i = 0; i < 45; i++)
        {
            hub.TickClock();
            int frame = s0.SimFrame;
            s0.InjectRemote(InputFramePacket.From(frame, 1, InputQuantizer.FromCardinal(-1, 0)));
            s1.InjectRemote(InputFramePacket.From(frame, 0, InputQuantizer.FromCardinal(1, 0)));
            Assert.That(s0.Advance(InputQuantizer.FromCardinal(1, 0)), Is.True);
            Assert.That(s1.Advance(InputQuantizer.FromCardinal(-1, 0)), Is.True);
        }

        Assert.That(s0.Sim.Hash(), Is.EqualTo(s1.Sim.Hash()));
        Assert.That(s0.DesyncFlagged, Is.False);
        Assert.That(s1.DesyncFlagged, Is.False);
    }

    [Test]
    public void Solo_remaining_peer_commits_handoff_at_disconnect_threshold()
    {
        var hub = new LoopbackHub { DeliveryDelayFrames = 0 };
        INetTransport t0 = hub.CreateEndpoint(0, new[] { 1 });
        var session = new RollbackSession(SessionConfig.Lan2P(0, inputDelay: 0), t0);
        session.Reset(MatchConfig.CreateDefault(Seed ^ 1UL, 2));

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
                Assert.That(advanced, Is.True);
                break;
            }
        }

        Assert.That(replaced, Is.True);
        Assert.That(session.PendingHandoffTarget, Is.EqualTo(-1));
    }
}
