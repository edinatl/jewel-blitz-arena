using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class ActorMatchRulesTests
{
    private const ulong Seed = 0x53315F4D41544348UL; // S1_MATCH

    [Test]
    public void Two_fighter_opening_starts_with_one_jewel_each()
    {
        var sim = CreateDuel();
        Assert.That(sim.State.Actor0.JewelsHeld, Is.EqualTo(1));
        Assert.That(sim.State.Actor1.JewelsHeld, Is.EqualTo(1));
        Assert.That(sim.State.JewelCap, Is.EqualTo(3));
    }

    [Test]
    public void Jewel_pickup_to_Blitz_sets_Luster_and_transform()
    {
        var sim = CreateDuel();
        sim.DebugGrantJewels(0, 2); // already held 1 → Blitz at 3
        Assert.That(sim.State.Actor0.IsBlitzed, Is.True);
        Assert.That(sim.State.Actor0.Luster, Is.EqualTo(MatchRules.LusterMax));
        Assert.That(sim.State.Actor0.TransformFrames, Is.EqualTo(MatchRules.BlitzTransformFrames));
        Assert.That(sim.State.Actor0.JewelsHeld, Is.EqualTo(0));
    }

    [Test]
    public void Blitz_Flare_then_hits_can_KO_and_end_match()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
            config.StartingLife[0] = 80;
            config.StartingLife[1] = 80;
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        sim.DebugGrantJewels(0, 2);

        // Skip transform invuln.
        for (int i = 0; i < MatchRules.BlitzTransformFrames + 1; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.Actor0.IsBlitzed, Is.True);
        Assert.That(sim.State.Actor0.TransformFrames, Is.EqualTo(0));

        // Place fighters in hit range and spam Flare (Attack while Blitzed).
        // Reset positions by restoring a mutated snapshot.
        var flare = InputFrame.Neutral;
        flare.Attack = true;

        for (int i = 0; i < 400 && !sim.MatchOver; i++)
        {
            // Keep fighters in melee range each frame (S1 has no chase AI).
            MatchState snap = sim.Snapshot();
            snap.Actor0.PosX = Fix.Zero;
            snap.Actor0.PosZ = Fix.Zero;
            snap.Actor0.Facing = 0;
            snap.Actor1.PosX = Fix.FromInt(1);
            snap.Actor1.PosZ = Fix.Zero;
            sim.Restore(snap);

            InputFrame atk = (i % 14 == 0) ? flare : InputFrame.Neutral;
            sim.Step(atk, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.MatchOver, Is.True, "Expected KO to end the duel");
        MatchResult result = sim.BuildResult();
        unsafe
        {
            Assert.That(result.Placements[0], Is.EqualTo(1));
            Assert.That(result.Placements[1], Is.EqualTo(2));
            Assert.That(result.Kos[0], Is.GreaterThanOrEqualTo(1));
        }
    }

    [Test]
    public void Luster_drains_while_Blitzed_and_reverts()
    {
        var sim = CreateDuel();
        sim.DebugGrantJewels(0, 2);

        // Finish transform, then drain a known number of frames.
        for (int i = 0; i < MatchRules.BlitzTransformFrames; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.Actor0.TransformFrames, Is.EqualTo(0));
        int lusterAfterTransform = sim.State.Actor0.Luster;

        for (int i = 0; i < 50; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.Actor0.IsBlitzed, Is.True);
        Assert.That(sim.State.Actor0.Luster, Is.EqualTo(lusterAfterTransform - 50));

        // Drain to zero.
        for (int i = 0; i < MatchRules.LusterMax; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            if (!sim.State.Actor0.IsBlitzed)
            {
                break;
            }
        }

        Assert.That(sim.State.Actor0.IsBlitzed, Is.False);
        Assert.That(sim.State.Actor0.DrossFrames, Is.GreaterThan(0));
    }

    [Test]
    public void Strong_hit_knocks_down_and_drops_a_jewel()
    {
        var sim = CreateDuel();
        Assert.That(sim.State.Actor1.JewelsHeld, Is.EqualTo(1));

        MatchState snap = sim.Snapshot();
        snap.Actor0.PosX = Fix.Zero;
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.PosX = Fix.FromInt(1);
        snap.Actor1.PosZ = Fix.Zero;
        sim.Restore(snap);

        var strong = InputFrame.Neutral;
        strong.Strong = true;
        sim.Step(strong, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        // Advance through startup into active.
        for (int i = 0; i < 20; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            if (sim.State.Actor1.KnockdownFrames > 0)
            {
                break;
            }
        }

        Assert.That(sim.State.Actor1.KnockdownFrames, Is.GreaterThan(0));
        Assert.That(sim.State.Actor1.JewelsHeld, Is.EqualTo(0));
    }

    [Test]
    public void Duel_script_is_hash_stable_across_replays()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        InputFrame[] log = BuildDuelLog(300);

        ulong a = Run(catalog, log);
        ulong b = Run(catalog, log);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.EqualTo(0UL));
    }

    private static Simulation CreateDuel()
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
        return sim;
    }

    private static ulong Run(ContentCatalog catalog, InputFrame[] p0Log)
    {
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
            config.StartingLife[0] = 200;
            config.StartingLife[1] = 200;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        for (int f = 0; f < p0Log.Length; f++)
        {
            InputFrame p1 = InputFrame.FromStick((p0Log[f].Direction + 16) & 31, p0Log[f].Magnitude);
            sim.Step(p0Log[f], p1, InputFrame.Neutral, InputFrame.Neutral);
        }

        return sim.Hash();
    }

    private static InputFrame[] BuildDuelLog(int frames)
    {
        var log = new InputFrame[frames];
        for (int f = 0; f < frames; f++)
        {
            log[f] = InputFrame.FromStick((f * 3) % 32, 2);
            if (f == 10)
            {
                // Will DebugGrant in a different test; here just press Attack periodically.
            }

            if (f % 20 == 5)
            {
                log[f].Attack = true;
            }

            if (f % 37 == 0)
            {
                log[f].Strong = true;
            }
        }

        return log;
    }
}
