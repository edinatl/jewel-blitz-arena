using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class S1S2IntegrationTests
{
    private const ulong Seed = 0x53312B53325F49UL; // S1+S2_I

    [Test]
    public void Field_jewel_pickup_emits_Blitz_via_contacts()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.Actor0.JewelsHeld, Is.EqualTo(1));

        // Drop both opening jewels, wait out pickup delay, walk onto a jewel twice more.
        sim.DebugDropOneJewel(0);
        sim.DebugDropOneJewel(1);

        for (int f = 0; f < PhysicsConstants.JewelPickupDelayFrames + 5; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        // Force actor onto first field jewel position.
        MatchState snap = sim.Snapshot();
        for (int i = 0; i < PoolCaps.Jewels; i++)
        {
            JewelSlot j = MatchStateAccess.GetJewel(ref snap, i);
            if (j.IsOnField && j.PickupDelayFrames == 0)
            {
                snap.Actor0.PosX = j.PosX;
                snap.Actor0.PosZ = j.PosZ;
                break;
            }
        }

        sim.Restore(snap);
        for (int f = 0; f < 5; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        // Grant remaining jewels via debug to complete Blitz path if only one field jewel collected.
        if (!sim.State.Actor0.IsBlitzed && sim.State.Actor0.JewelsHeld < 3)
        {
            sim.DebugGrantJewels(0, 3 - sim.State.Actor0.JewelsHeld);
        }

        Assert.That(sim.State.Actor0.IsBlitzed, Is.True);
        Assert.That(sim.State.Actor0.Luster, Is.EqualTo(MatchRules.LusterMax));
    }

    [Test]
    public void Thrown_item_contact_applies_S1_damage()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
            config.StartingLife[0] = 200;
            config.StartingLife[1] = 200;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        int itemId = SampleContentFactory.CreateS5StubPack().Items[0].ItemId;
        sim.SpawnLooseItem(itemId, Fix.Zero, Fix.Zero, Fix.Zero);

        // P0 picks up and throws toward P1.
        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(2);
        snap.Actor1.PosZ = Fix.Zero;
        sim.Restore(snap);

        for (int f = 0; f < 15; f++)
        {
            var p0 = InputFrame.Neutral;
            p0.Action = true;
            sim.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.Actor0.HasEmptyHands, Is.False);

        int lifeBefore = sim.State.Actor1.Life;
        for (int f = 0; f < 60; f++)
        {
            var p0 = InputFrame.FromStick(0, 3); // +X
            p0.Attack = f < 3;
            // Keep victim near throw line.
            MatchState s = sim.Snapshot();
            s.Actor1.PosX = Fix.FromInt(2);
            s.Actor1.PosZ = Fix.Zero;
            sim.Restore(s);
            sim.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            if (sim.State.Actor1.Life < lifeBefore)
            {
                break;
            }
        }

        Assert.That(sim.State.Actor1.Life, Is.LessThan(lifeBefore));
    }

    [Test]
    public void Integrated_duel_script_is_hash_stable()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        InputFrame[] log = new InputFrame[240];
        for (int f = 0; f < log.Length; f++)
        {
            log[f] = InputFrame.FromStick((f * 3) % 32, 2);
            if (f % 20 == 5) log[f].Attack = true;
            if (f % 37 == 0) log[f].Strong = true;
            if (f % 41 == 0) log[f].Action = true;
        }

        ulong a = Run(catalog, log);
        ulong b = Run(catalog, log);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(0UL));
    }

    private static ulong Run(ContentCatalog catalog, InputFrame[] p0Log)
    {
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        sim.SpawnCrateItemAt(Fix.FromInt(1), Fix.FromInt(1));
        for (int f = 0; f < p0Log.Length; f++)
        {
            InputFrame p1 = InputFrame.FromStick((p0Log[f].Direction + 16) & 31, p0Log[f].Magnitude);
            sim.Step(p0Log[f], p1, InputFrame.Neutral, InputFrame.Neutral);
        }

        return sim.Hash();
    }
}
