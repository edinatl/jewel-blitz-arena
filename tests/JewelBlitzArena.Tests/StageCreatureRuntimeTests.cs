using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class StageCreatureRuntimeTests
{
    private const ulong Seed = 0x5645494E5F475244UL; // VEIN_GRD

    [Test]
    public void Pressworks_arms_five_vein_guards()
    {
        var sim = ResetPressworks(Seed);
        MatchState snap = sim.Snapshot();
        Assert.That(StageCreatureRuntime.CountActive(ref snap), Is.EqualTo(5));
        Assert.That(StageCreatureRuntime.CountDestroyed(ref snap), Is.EqualTo(0));
    }

    [Test]
    public void Other_stages_have_no_creatures()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        MatchState snap = sim.Snapshot();
        Assert.That(StageCreatureRuntime.CountActive(ref snap), Is.EqualTo(0));
    }

    [Test]
    public void Attack_destroys_guard_and_counts()
    {
        var sim = ResetPressworks(Seed ^ 2UL);
        MatchState snap = sim.Snapshot();
        BreakCreature(sim, ref snap, 0);
        snap = sim.Snapshot();
        Assert.That(StageCreatureRuntime.CountDestroyed(ref snap), Is.EqualTo(1));
        Assert.That(StageCreatureRuntime.CountActive(ref snap), Is.EqualTo(4));
        Assert.That(
            (MatchStateAccess.GetStageCreature(ref snap, 0).Flags & StageCreatureRuntime.FlagBroken) != 0,
            Is.True);
    }

    [Test]
    public void Four_destroyed_opens_sorting_gate_early()
    {
        var sim = ResetPressworks(Seed ^ 3UL, phaseClockPermille: 1000);
        MatchState snap = sim.Snapshot();
        Assert.That(snap.PhaseIndex, Is.EqualTo(0));
        for (int i = 0; i < 4; i++)
        {
            BreakCreature(sim, ref snap, i);
            snap = sim.Snapshot();
        }

        // The Step that breaks the 4th guard also advances the sorting gate and clears creatures.
        Assert.That(snap.PhaseIndex, Is.GreaterThanOrEqualTo(1),
            "four destroys should open the gate without waiting 90 s");
        Assert.That(StageCreatureRuntime.CountActive(ref snap), Is.EqualTo(0));
    }

    [Test]
    public void Timer_gate_still_advances_without_kills()
    {
        // 90 s @ 10% clock = 540 f + 12 f transition.
        var sim = ResetPressworks(Seed ^ 4UL, phaseClockPermille: 100);
        for (int f = 0; f < 540 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState press = sim.Snapshot();
        Assert.That(press.PhaseIndex, Is.EqualTo(1));
        Assert.That(StageCreatureRuntime.CountActive(ref press), Is.EqualTo(0));
    }

    [Test]
    public void Destroying_each_guard_drops_matching_stage_weapon()
    {
        int[] expectedIds =
        {
            MatchRules.StageMortarItemId,
            MatchRules.StageFrostStaffItemId,
            MatchRules.StageMalletItemId,
            MatchRules.StageNetItemId,
            MatchRules.StageFlameItemId,
        };

        // Kill one at a time on fresh resets so the sorting gate does not clear mid-test.
        for (int kindIndex = 0; kindIndex < expectedIds.Length; kindIndex++)
        {
            var sim = ResetPressworks(Seed ^ (ulong)(0x10 + kindIndex));
            MatchState snap = sim.Snapshot();
            int before = snap.ActiveLooseItemCount;
            GenericPoolSlot guard = MatchStateAccess.GetStageCreature(ref snap, kindIndex);
            Fix gx = guard.PosX;
            Fix gz = guard.PosZ;

            BreakCreature(sim, ref snap, kindIndex);
            snap = sim.Snapshot();

            Assert.That(snap.ActiveLooseItemCount, Is.EqualTo(before + 1));
            Assert.That(
                (MatchStateAccess.GetStageCreature(ref snap, kindIndex).Flags
                    & StageCreatureRuntime.FlagBroken) != 0,
                Is.True,
                "Broken flag must remain for gate counting");

            bool found = false;
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref snap, i);
                if (!item.IsActive || item.ItemId != expectedIds[kindIndex])
                {
                    continue;
                }

                found = true;
                Assert.That(item.IsStageOnly, Is.True);
                Assert.That(item.PosX.Raw, Is.EqualTo(gx.Raw));
                Assert.That(item.PosZ.Raw, Is.EqualTo(gz.Raw));
                break;
            }

            Assert.That(found, Is.True, $"expected stage weapon {expectedIds[kindIndex]}");
        }
    }

    [Test]
    public void Four_destroys_still_opens_gate_with_drops_present()
    {
        var sim = ResetPressworks(Seed ^ 5UL, phaseClockPermille: 1000);
        MatchState snap = sim.Snapshot();
        for (int i = 0; i < 4; i++)
        {
            BreakCreature(sim, ref snap, i);
            snap = sim.Snapshot();
        }

        Assert.That(snap.PhaseIndex, Is.GreaterThanOrEqualTo(1));
        Assert.That(snap.ActiveLooseItemCount, Is.GreaterThanOrEqualTo(4),
            "stage weapons should remain after gate clears creatures");
    }

    private static Simulation ResetPressworks(ulong seed, int phaseClockPermille = 1000)
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.PressworksStageId;
            config.PhaseClockPermille = phaseClockPermille;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        return sim;
    }

    private static void BreakCreature(Simulation sim, ref MatchState snap, int index)
    {
        int remaining = MatchStateAccess.GetStageCreature(ref snap, index).Param0;
        while (remaining > 0)
        {
            GenericPoolSlot creature = MatchStateAccess.GetStageCreature(ref snap, index);
            {
                ref ActorSlot a0 = ref MatchStateAccess.Actor(ref snap, 0);
                a0.PosX = creature.PosX;
                a0.PosZ = creature.PosZ;
                a0.PosY = Fix.Zero;
                a0.Action = (byte)ActorAction.Strong;
                a0.ActionPhase = (byte)ActionPhase.Active;
                a0.ActionTimer = 20;
                a0.HitApplied = 0;
                a0.MoveDamage = remaining > 90 ? 90 : remaining;

                ref ActorSlot a1 = ref MatchStateAccess.Actor(ref snap, 1);
                a1.PosX = Fix.FromInt(40);
                a1.PosZ = Fix.FromInt(40);
            }

            sim.Restore(snap);
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            snap = sim.Snapshot();
            creature = MatchStateAccess.GetStageCreature(ref snap, index);
            if ((creature.Flags & StageCreatureRuntime.FlagBroken) != 0)
            {
                return;
            }

            remaining = creature.Param0;
        }
    }
}
