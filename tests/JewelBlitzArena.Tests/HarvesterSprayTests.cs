using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class HarvesterSprayTests
{
    private const ulong Seed = 0x53505241595F48UL; // SPRAY_H

    [Test]
    public void Spray_arms_on_Harvester_reset()
    {
        var sim = ResetHarvester(Seed);
        MatchState snap = sim.Snapshot();
        Assert.That(snap.ActiveHazardCount, Is.EqualTo(6));

        bool left = false;
        bool right = false;
        for (int i = 0; i < snap.ActiveHazardCount; i++)
        {
            GenericPoolSlot h = MatchStateAccess.GetHazard(ref snap, i);
            if (h.TypeId == StageRuntime.AbrasiveSprayHazardIdL)
            {
                left = true;
            }

            if (h.TypeId == StageRuntime.AbrasiveSprayHazardIdR)
            {
                right = true;
            }
        }

        Assert.That(left, Is.True);
        Assert.That(right, Is.True);
    }

    [Test]
    public void Actor_inside_Spray_loses_50_Life_per_second_without_stun()
    {
        var sim = ResetHarvester(Seed ^ 1UL);
        MatchState snap = sim.Snapshot();

        // Place actor 0 inside left nozzle (−6, 0), radius 2.5.
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = Fix.FromInt(-6);
            a0.PosZ = Fix.Zero;
            a0.PosY = Fix.Zero;
            a0.InvulnFrames = 0;
            a0.StunFrames = 0;
            int lifeBefore = a0.Life;

            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(40);
            a1.PosZ = Fix.FromInt(40);
            a1.InvulnFrames = 0;

            sim.Restore(snap);

            // PeriodSeconds = 1 → 60 frames to first pulse.
            for (int i = 0; i < 60; i++)
            {
                sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            }

            MatchState after = sim.Snapshot();
            ref ActorSlot hit = ref MatchStateAccess.Actor(ref after, 0);
            Assert.That(hit.Life, Is.EqualTo(lifeBefore - 50));
            Assert.That(hit.StunFrames, Is.EqualTo(0), "Spray must not apply hit-stun");

            ref ActorSlot far = ref MatchStateAccess.Actor(ref after, 1);
            Assert.That(far.Life, Is.EqualTo(MatchStateAccess.Actor(ref snap, 1).Life));
        }
    }

    [Test]
    public void Actor_outside_Spray_takes_no_damage()
    {
        var sim = ResetHarvester(Seed ^ 2UL);
        MatchState snap = sim.Snapshot();

        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
            a0.PosX = Fix.Zero;
            a0.PosZ = Fix.Zero;
            a0.InvulnFrames = 0;
            int lifeBefore = a0.Life;

            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
            a1.PosX = Fix.FromInt(40);
            a1.PosZ = Fix.FromInt(40);

            sim.Restore(snap);
            for (int i = 0; i < 60; i++)
            {
                sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            }

            MatchState after = sim.Snapshot();
            // Center (0,0) is outside both ±6 radius-2.5 sprays; other hazards may still pulse.
            // Assert only that Spray itself did not fire on this actor by checking Life drop
            // is not exactly the Spray 50 from nozzle overlap (actor far from nozzles).
            // Footfall etc. have longer periods — at frame 60 only Spray pulses.
            Assert.That(MatchStateAccess.Actor(ref after, 0).Life, Is.EqualTo(lifeBefore));
        }
    }

    private static Simulation ResetHarvester(ulong seed)
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        return sim;
    }
}
