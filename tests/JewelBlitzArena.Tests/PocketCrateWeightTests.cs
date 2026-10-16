using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class PocketCrateWeightTests
{
    private const ulong Seed = 0x504F434B455457UL; // POCKETW

    [Test]
    public void BaseWeightForRarity_matches_design_table()
    {
        Assert.That(CrateSpawner.BaseWeightForRarity(1), Is.EqualTo(100));
        Assert.That(CrateSpawner.BaseWeightForRarity(2), Is.EqualTo(70));
        Assert.That(CrateSpawner.BaseWeightForRarity(3), Is.EqualTo(45));
        Assert.That(CrateSpawner.BaseWeightForRarity(4), Is.EqualTo(25));
        Assert.That(CrateSpawner.BaseWeightForRarity(5), Is.EqualTo(12));
        Assert.That(CrateSpawner.BaseWeightForRarity(6), Is.EqualTo(5));
        Assert.That(CrateSpawner.BaseWeightForRarity(7), Is.EqualTo(2));
    }

    [Test]
    public void Custom_ApplyPockets_when_HouseRule_on()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(1);
        profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
        Assert.That(LedgerRuntime.TryPocket(profile, catalog, 1), Is.True);
        Assert.That(LedgerRuntime.TryPocket(profile, catalog, SampleContentFactory.SolitaireItemId), Is.True);

        var orch = new CustomModeOrchestrator();
        var off = new ModeRequest
        {
            Mode = ModeId.Custom,
            Seed = Seed,
            ActorCount = 2,
            HouseRules = HouseRulesSettings.CreateDefaults(), // PocketItems false
        };
        MatchConfig empty = orch.BuildConfig(profile, off);
        Assert.That(empty.PocketItemCount, Is.EqualTo(0));

        var on = off;
        on.HouseRules = new HouseRulesSettings
        {
            PocketItems = true,
            LusterTimer = true,
            JewelCap = 0,
            BlitzedDamageTakenPercent = 50,
            BlitzedAttackPercent = 100,
            RallySeconds = 9,
            PhaseClockPercent = 100,
        };
        MatchConfig filled = orch.BuildConfig(profile, on);
        Assert.That(filled.PocketItemCount, Is.EqualTo(2));
        Assert.That(filled.ContainsPocketItem(1), Is.True);
        Assert.That(filled.ContainsPocketItem(SampleContentFactory.SolitaireItemId), Is.True);
    }

    [Test]
    public void Expedition_never_copies_pockets()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();
        profile.Ledger.Grant(SampleContentFactory.SolitaireItemId);
        LedgerRuntime.TryPocket(profile, catalog, SampleContentFactory.SolitaireItemId);

        var orch = new ExpeditionModeOrchestrator();
        var request = new ModeRequest
        {
            Mode = ModeId.Expedition,
            Seed = Seed,
            StageId = SampleContentFactory.CitrineBazaarStageId,
            ActorCount = 2,
            Difficulty = 1,
            HouseRules = new HouseRulesSettings
            {
                PocketItems = true,
                LusterTimer = true,
                JewelCap = 0,
                BlitzedDamageTakenPercent = 50,
                BlitzedAttackPercent = 100,
                RallySeconds = 9,
                PhaseClockPercent = 100,
            },
        };
        MatchConfig config = orch.BuildConfig(profile, request);
        Assert.That(config.PocketItemCount, Is.EqualTo(0));
    }

    [Test]
    public void Roll_empty_pockets_stays_uniform_AlwaysEligible()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var sim = new Simulation();
        MatchConfig config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        sim.Reset(config, catalog);
        MatchState state = sim.Snapshot();

        int id = CrateSpawner.RollAlwaysEligible(ref state, catalog);
        Assert.That(id, Is.GreaterThan(0));
        Assert.That(catalog.TryGetItem(id, out ItemRecord item), Is.True);
        Assert.That(item.CrateEligibility, Is.EqualTo(CrateEligibility.Always));
    }

    [Test]
    public void Roll_pocketed_Never_Solitaire_can_appear()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetItem(SampleContentFactory.SolitaireItemId, out ItemRecord solitaire), Is.True);
        Assert.That(solitaire.CrateEligibility, Is.EqualTo(CrateEligibility.Never));

        var pockets = new[] { SampleContentFactory.SolitaireItemId };
        int hits = 0;
        const int Trials = 4000;
        for (int t = 0; t < Trials; t++)
        {
            var sim = new Simulation();
            MatchConfig config = MatchConfig.CreateDefault(Seed ^ (ulong)(t + 1), actorCount: 2);
            config.SetPocketItems(pockets);
            sim.Reset(config, catalog);
            MatchState state = sim.Snapshot();
            int id = CrateSpawner.RollAlwaysEligible(ref state, catalog, pockets, 1);
            if (id == SampleContentFactory.SolitaireItemId)
            {
                hits++;
            }
        }

        Assert.That(hits, Is.GreaterThan(0), "pocketed Never item must join the crate pool");
    }

    [Test]
    public void Roll_pocketed_Always_item_appears_more_often_than_unpocketed()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        const int TargetId = 1; // Pistol, rarity 1, Always
        var pockets = new[] { TargetId };
        int withPocket = CountRolls(catalog, Seed, pockets, TargetId, trials: 3000);
        int without = CountRolls(catalog, Seed, null, TargetId, trials: 3000);
        Assert.That(withPocket, Is.GreaterThan(without),
            $"pocket ×3 should bias rolls toward id {TargetId}: pocketed={withPocket} plain={without}");
    }

    [Test]
    public void Simulation_Reset_feeds_pockets_into_SpawnCrateItemAt()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        MatchConfig config = MatchConfig.CreateDefault(Seed ^ 0xACEUL, actorCount: 2);
        config.SetPocketItems(new[] { SampleContentFactory.SolitaireItemId });
        var sim = new Simulation();

        // Rarity-7 ×3 vs Always pool ≈ 0.5%; need enough trials for one hit.
        bool sawSolitaire = false;
        for (int i = 0; i < 5000; i++)
        {
            config = MatchConfig.CreateDefault(Seed ^ (ulong)(i * 31 + 7), actorCount: 2);
            config.SetPocketItems(new[] { SampleContentFactory.SolitaireItemId });
            sim.Reset(config, catalog);
            int slot = sim.SpawnCrateItemAt(Fix.Zero, Fix.Zero);
            Assert.That(slot, Is.GreaterThanOrEqualTo(0));
            MatchState snap = sim.Snapshot();
            LooseItemSlot item = MatchStateAccess.GetLooseItem(ref snap, slot);
            if (item.ItemId == SampleContentFactory.SolitaireItemId)
            {
                sawSolitaire = true;
                break;
            }
        }

        Assert.That(sawSolitaire, Is.True);
    }

    private static int CountRolls(
        ContentCatalog catalog, ulong seed, int[]? pockets, int targetId, int trials)
    {
        int hits = 0;
        int pocketCount = pockets?.Length ?? 0;
        for (int t = 0; t < trials; t++)
        {
            var sim = new Simulation();
            MatchConfig config = MatchConfig.CreateDefault(seed ^ (ulong)(t * 17 + 3), actorCount: 2);
            if (pockets != null)
            {
                config.SetPocketItems(pockets);
            }

            sim.Reset(config, catalog);
            MatchState state = sim.Snapshot();
            int id = CrateSpawner.RollAlwaysEligible(ref state, catalog, pockets, pocketCount);
            if (id == targetId)
            {
                hits++;
            }
        }

        return hits;
    }
}
