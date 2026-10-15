using System.Diagnostics;
using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class PhysicsJewelsItemsTests
{
    private const ulong Seed = 0x53325F50485953UL; // S2_PHYS

    [Test]
    public void Two_actor_match_opens_with_one_jewel_each_and_cap_3()
    {
        var sim = new Simulation();
        sim.Reset(MatchConfig.CreateDefault(Seed, actorCount: 2));
        MatchState snap = sim.Snapshot();

        Assert.That(snap.JewelCap, Is.EqualTo(3));
        Assert.That(snap.Actor0.JewelsHeld, Is.EqualTo(1));
        Assert.That(snap.Actor1.JewelsHeld, Is.EqualTo(1));
        Assert.That(JewelRuntime.CountTowardCap(ref snap), Is.EqualTo(2));
    }

    [Test]
    public void Jewel_scramble_is_hash_stable_across_two_sims()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        ulong a = RunScrambleHash(Seed, catalog);
        ulong b = RunScrambleHash(Seed, catalog);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void Thrown_item_and_pickup_are_hash_stable()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        int itemId = SampleContentFactory.CreateS5StubPack().Items[0].ItemId;

        ulong Run()
        {
            var config = MatchConfig.CreateDefault(Seed ^ 0x11UL, actorCount: 2);
            var sim = new Simulation();
            sim.Reset(config, catalog);
            int slot = sim.SpawnLooseItem(itemId, Fix.Zero, Fix.Zero, Fix.Zero);
            Assert.That(slot, Is.GreaterThanOrEqualTo(0));

            for (int f = 0; f < 120; f++)
            {
                var p0 = InputFrame.FromStick(0, 2);
                if (f >= 10 && f < 20)
                {
                    p0.Action = true;
                }

                if (f >= 40 && f < 45)
                {
                    p0.Attack = true;
                    p0.Direction = 8;
                    p0.Magnitude = 3;
                }

                sim.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            }

            return sim.Hash();
        }

        ulong hashA = Run();
        ulong hashB = Run();
        Assert.That(hashA, Is.EqualTo(hashB));
    }

    [Test]
    public void Drop_jewel_api_scatters_onto_field_with_pickup_delay()
    {
        var sim = new Simulation();
        sim.Reset(MatchConfig.CreateDefault(Seed, actorCount: 2));
        Assert.That(sim.State.Actor0.JewelsHeld, Is.EqualTo(1));
        Assert.That(sim.DebugDropOneJewel(0), Is.True);
        Assert.That(sim.State.Actor0.JewelsHeld, Is.EqualTo(0));

        MatchState snap = sim.Snapshot();
        Assert.That(JewelRuntime.CountTowardCap(ref snap), Is.EqualTo(2));

        bool foundDelayed = false;
        for (int i = 0; i < PoolCaps.Jewels; i++)
        {
            JewelSlot j = MatchStateAccess.GetJewel(ref snap, i);
            if (j.IsOnField && j.PickupDelayFrames > 0)
            {
                foundDelayed = true;
            }
        }

        Assert.That(foundDelayed, Is.True);
    }

    [Test]
    public void Circulation_cap_blocks_extra_spawns()
    {
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        config.JewelCap = 2;
        var sim = new Simulation();
        sim.Reset(config);

        MatchState snap = sim.Snapshot();
        Assert.That(JewelRuntime.CountTowardCap(ref snap), Is.EqualTo(2));

        for (int f = 0; f < PhysicsConstants.JewelOpenSpawnFrame + 10; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        snap = sim.Snapshot();
        Assert.That(JewelRuntime.CountTowardCap(ref snap), Is.EqualTo(2));
    }

    [Test]
    public void Pool_caps_are_enforced_for_loose_items()
    {
        var sim = new Simulation();
        sim.Reset(MatchConfig.CreateDefault(Seed, actorCount: 2));

        int spawned = 0;
        for (int i = 0; i < PoolCaps.LooseItems + 4; i++)
        {
            int slot = sim.SpawnLooseItem(1, Fix.FromInt(i % 5), Fix.Zero, Fix.FromInt(i % 3));
            if (slot >= 0)
            {
                spawned++;
            }
        }

        Assert.That(spawned, Is.EqualTo(PoolCaps.LooseItems));
        Assert.That(sim.State.ActiveLooseItemCount, Is.EqualTo(PoolCaps.LooseItems));
    }

    [Test]
    public void Step_budget_smoke_under_half_millisecond_average()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 4);
        var sim = new Simulation();
        sim.Reset(config, catalog);
        sim.SpawnCrateItemAt(Fix.FromInt(2), Fix.FromInt(2));

        for (int i = 0; i < 30; i++)
        {
            sim.Step(
                InputFrame.FromStick(i % 32, 2),
                InputFrame.FromStick((i * 3) % 32, 2),
                InputFrame.FromStick((i * 5) % 32, 1),
                InputFrame.FromStick((i * 7) % 32, 1));
        }

        const int frames = 600;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++)
        {
            sim.Step(
                InputFrame.FromStick(i % 32, 2),
                InputFrame.FromStick((i * 3) % 32, 2),
                InputFrame.FromStick((i * 5) % 32, 1),
                InputFrame.FromStick((i * 7) % 32, 1));
        }

        sw.Stop();
        double avgMs = sw.Elapsed.TotalMilliseconds / frames;
        TestContext.WriteLine($"S2 avg step {avgMs:F4} ms over {frames} frames");
        Assert.That(avgMs, Is.LessThan(0.5), "Early budget smoke — target 0.35 ms later");
    }

    private static ulong RunScrambleHash(ulong seed, ContentCatalog catalog)
    {
        var config = MatchConfig.CreateDefault(seed, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config, catalog);

        sim.DebugDropOneJewel(0);
        sim.DebugDropOneJewel(1);

        for (int f = 0; f < 300; f++)
        {
            var p0 = InputFrame.FromStick((f * 2) % 32, 2);
            var p1 = InputFrame.FromStick((f * 5 + 8) % 32, 2);
            if ((f % 40) == 0)
            {
                p0.Action = true;
            }

            sim.Step(p0, p1, InputFrame.Neutral, InputFrame.Neutral);
        }

        return sim.Hash();
    }
}
