using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class SolitaireJewelTests
{
    private const ulong Seed = 0x534F4C49544149UL; // SOLITAI

    [Test]
    public void MatchRules_SolitaireItemId_matches_catalog()
    {
        Assert.That(MatchRules.SolitaireItemId, Is.EqualTo(SampleContentFactory.SolitaireItemId));
        Assert.That(Meta.SolitaireRuntime.ItemId, Is.EqualTo(SampleContentFactory.SolitaireItemId));
    }

    [Test]
    public void Meta_TryPocket_Solitaire_when_owned()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        Assert.That(Meta.SolitaireRuntime.TryPocket(profile, catalog), Is.False);
        profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
        Assert.That(Meta.SolitaireRuntime.TryPocket(profile, catalog), Is.True);
        Assert.That(Meta.SolitaireRuntime.IsPocketed(profile), Is.True);
        Assert.That(Meta.SolitaireRuntime.TryUnpocket(profile), Is.True);
        Assert.That(Meta.SolitaireRuntime.IsPocketed(profile), Is.False);
    }

    [Test]
    public void Sim_TryUse_grants_bonus_jewel_toward_Blitz()
    {
        var sim = new Simulation();
        sim.Reset(MatchConfig.CreateDefault(Seed, actorCount: 2));
        MatchState state = sim.Snapshot();
        int before = state.Actor0.JewelsHeld;

        Assert.That(Sim.SolitaireRuntime.TryUse(ref state, 0), Is.True);
        Assert.That(state.Actor0.JewelsHeld, Is.EqualTo(before + 1));

        while (state.Actor0.JewelsHeld < MatchRules.BlitzJewelCount)
        {
            if (!Sim.SolitaireRuntime.TryUse(ref state, 0))
            {
                break;
            }
        }

        if (state.Actor0.JewelsHeld >= MatchRules.BlitzJewelCount)
        {
            Assert.That(Sim.SolitaireRuntime.TryUse(ref state, 0), Is.False, "already at Blitz count");
        }
    }

    [Test]
    public void Sim_TryUse_emits_JewelReadyToBlitz_at_threshold()
    {
        var sim = new Simulation();
        // 4P opens with no starting jewels; cap 5 leaves room for Solitaire grants.
        sim.Reset(MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 4));
        MatchState state = sim.Snapshot();
        Assert.That(state.Actor0.JewelsHeld, Is.EqualTo(0));

        var contacts = default(SimContactBuffer);
        Assert.That(Sim.SolitaireRuntime.TryUse(ref state, 0, ref contacts), Is.True);
        Assert.That(state.Actor0.JewelsHeld, Is.EqualTo(1));

        contacts = default;
        Assert.That(Sim.SolitaireRuntime.TryUse(ref state, 0, ref contacts), Is.True);
        Assert.That(state.Actor0.JewelsHeld, Is.EqualTo(2));

        contacts = default;
        Assert.That(Sim.SolitaireRuntime.TryUse(ref state, 0, ref contacts), Is.True);
        Assert.That(state.Actor0.JewelsHeld, Is.EqualTo(3));

        bool ready = false;
        for (int i = 0; i < contacts.Count; i++)
        {
            if (contacts.Get(i).Kind == SimEventKind.JewelReadyToBlitz)
            {
                ready = true;
                break;
            }
        }

        Assert.That(ready, Is.True);
    }
}
