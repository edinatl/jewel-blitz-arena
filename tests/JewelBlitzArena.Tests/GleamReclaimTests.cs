using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class GleamReclaimTests
{
    private const ulong Seed = 0x474C4D5F52434C4DUL; // GLM_RCLM

    [Test]
    public void KnockOut_drops_held_jewels_to_field()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        config.JewelCap = 6;
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        MatchState before = sim.Snapshot();
        // Opening grants 1 owned jewel; add one more owned slot (cap 6).
        Assert.That(JewelRuntime.TryGiveOwnedJewel(ref before, 0), Is.True);
        Assert.That(MatchStateAccess.Actor(ref before, 0).JewelsHeld, Is.EqualTo(2));
        int fieldBefore = CountFieldJewels(ref before);

        {
            ref ActorSlot victim = ref MatchStateAccess.Actor(ref before, 0);
            victim.Life = 1;
            victim.PosX = Fix.FromInt(2);
            victim.PosZ = Fix.Zero;
            ref ActorSlot atk = ref MatchStateAccess.Actor(ref before, 1);
            atk.PosX = Fix.FromInt(2);
            atk.PosZ = Fix.Zero;
            atk.Action = (byte)ActorAction.Attack;
            atk.ActionPhase = (byte)ActionPhase.Active;
            atk.ActionTimer = 10;
            atk.HitApplied = 0;
            atk.MoveDamage = 50;
        }

        sim.Restore(before);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(MatchStateAccess.Actor(ref after, 0).IsEliminated, Is.True);
        Assert.That(MatchStateAccess.Actor(ref after, 0).JewelsHeld, Is.EqualTo(0));
        int fieldAfter = CountFieldJewels(ref after);
        Assert.That(fieldAfter, Is.GreaterThanOrEqualTo(fieldBefore + 2));
    }

    [Test]
    public void Expedition_KO_spawns_copper_gleam()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        config.HouseRulesBits = MatchRules.HouseLusterTimer | MatchRules.ExpeditionGleams;
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        MatchState before = sim.Snapshot();
        Assert.That(GleamRuntime.CountActive(ref before), Is.EqualTo(0));

        {
            ref ActorSlot victim = ref MatchStateAccess.Actor(ref before, 0);
            victim.Life = 1;
            victim.PosX = Fix.FromInt(4);
            victim.PosZ = Fix.FromInt(1);
            victim.JewelsHeld = 0;
            ref ActorSlot atk = ref MatchStateAccess.Actor(ref before, 1);
            atk.PosX = Fix.FromInt(4);
            atk.PosZ = Fix.FromInt(1);
            atk.Action = (byte)ActorAction.Attack;
            atk.ActionPhase = (byte)ActionPhase.Active;
            atk.ActionTimer = 10;
            atk.HitApplied = 0;
            atk.MoveDamage = 50;
        }

        sim.Restore(before);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        Assert.That(MatchStateAccess.Actor(ref after, 0).IsEliminated, Is.True);
        Assert.That(GleamRuntime.CountActive(ref after), Is.EqualTo(1));
    }

    [Test]
    public void World_patches_ignore_gleam_slots()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 2UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        MatchState snap = sim.Snapshot();
        // Spawn far from fighters so Tick does not auto-pickup.
        Assert.That(
            GleamRuntime.TrySpawnCopper(ref snap, Fix.FromInt(20), Fix.FromInt(20)),
            Is.GreaterThanOrEqualTo(0));
        sim.Restore(snap);

        for (int f = 0; f < 5; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState after = sim.Snapshot();
        Assert.That(GleamRuntime.CountActive(ref after), Is.EqualTo(1));
        Assert.That(WorldPatchRuntime.CountActive(ref after), Is.EqualTo(0));
    }

    private static int CountFieldJewels(ref MatchState state)
    {
        int n = 0;
        for (int i = 0; i < PoolCaps.Jewels; i++)
        {
            JewelSlot j = MatchStateAccess.GetJewel(ref state, i);
            if (j.IsOnField)
            {
                n++;
            }
        }

        return n;
    }
}
