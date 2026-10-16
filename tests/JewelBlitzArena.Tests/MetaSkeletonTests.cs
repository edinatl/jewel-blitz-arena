using JewelBlitzArena.Data;
using JewelBlitzArena.Meta;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class MetaSkeletonTests
{
    private const ulong Seed = 0x53365F4D455441UL; // S6_META

    [Test]
    public void Profile_migrate_v0_to_v1_is_noop_fill()
    {
        PlayerProfile v0 = ProfileStore.CreateV0Fixture();
        Assert.That(v0.SchemaVersion, Is.EqualTo(ProfileSchema.Version0));

        ProfileMigrator.MigrateToCurrent(v0);
        Assert.That(v0.SchemaVersion, Is.EqualTo(ProfileSchema.Version1));
        Assert.That(v0.DisplayName, Is.EqualTo("Rookie"));
        Assert.That(v0.HouseRulesDefaults.LusterTimer, Is.True);
        Assert.That(v0.UnlockedHouseRulesBits & HouseRulesBits.LusterTimer, Is.Not.EqualTo(0));
    }

    [Test]
    public void ProfileStore_round_trip_atomic_save()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-meta-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var store = new ProfileStore(path);
            PlayerProfile profile = PlayerProfile.CreateNew("Ed");
            profile.Carats = 42;
            profile.Ledger.Grant(1001);
            store.Save(profile);

            Assert.That(File.Exists(path), Is.True);

            PlayerProfile loaded = store.LoadOrCreate();
            Assert.That(loaded.DisplayName, Is.EqualTo("Ed"));
            Assert.That(loaded.Carats, Is.EqualTo(42));
            Assert.That(loaded.Ledger.Owns(1001), Is.True);
            Assert.That(loaded.SchemaVersion, Is.EqualTo(ProfileSchema.Current));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void Custom_builds_MatchConfig_with_HouseRules_Luster_bit()
    {
        var profile = PlayerProfile.CreateNew();
        var orch = new CustomModeOrchestrator();
        var request = new ModeRequest
        {
            Mode = ModeId.Custom,
            Seed = Seed,
            ActorCount = 2,
            HouseRules = HouseRulesSettings.CreateDefaults(),
        };

        MatchConfig config = orch.BuildConfig(profile, request);
        Assert.That(config.Seed, Is.EqualTo(Seed));
        Assert.That(config.ActorCount, Is.EqualTo(2));
        Assert.That(config.HouseRulesBits & MatchRules.HouseLusterTimer, Is.Not.EqualTo(0));

        request.HouseRules = new HouseRulesSettings
        {
            LusterTimer = false,
            JewelCap = 7,
            BlitzedDamageTakenPercent = 50,
            BlitzedAttackPercent = 100,
            RallySeconds = 9,
            PhaseClockPercent = 100,
        };
        config = orch.BuildConfig(profile, request);
        Assert.That(config.HouseRulesBits & MatchRules.HouseLusterTimer, Is.EqualTo(0));
        Assert.That(config.JewelCap, Is.EqualTo(7));
    }

    [Test]
    public void Sparring_caps_actors_at_2_and_uses_training_stage()
    {
        var profile = PlayerProfile.CreateNew();
        var orch = new SparringModeOrchestrator();
        var request = new ModeRequest { Mode = ModeId.Sparring, Seed = Seed, ActorCount = 4 };
        MatchConfig config = orch.BuildConfig(profile, request);
        Assert.That(config.ActorCount, Is.EqualTo(2));
        Assert.That(config.StageId, Is.EqualTo(SparringModeOrchestrator.TrainingStageId));
    }

    [Test]
    public void Boot_Custom_greybox_writes_inventory_flag_and_preserves_sim_hash()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-meta-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var boot = new MetaBoot(new ProfileStore(path));
            boot.Boot();
            Assert.That(boot.Profile.HasFlag(ProfileFlags.CompletedCustomGreybox), Is.False);

            var request = new ModeRequest { Mode = ModeId.Custom, Seed = Seed, ActorCount = 2 };
            MetaGreyboxRunResult run = boot.RunCustomGreybox(frames: 60, request);

            Assert.That(run.OwnsGreyboxToken, Is.True);
            Assert.That(boot.Profile.HasFlag(ProfileFlags.CompletedCustomGreybox), Is.True);
            Assert.That(boot.Profile.Ledger.Owns(LedgerInventory.CustomGreyboxTokenItemId), Is.True);
            Assert.That(run.Hash, Is.Not.EqualTo(0UL));

            // Re-run same seed/frames outside Meta — hash must match (Meta did not touch Sim rules).
            MatchConfig config = MatchConfig.CreateDefault(Seed, 2);
            config.HouseRulesBits = HouseRulesBits.ToSimBits(HouseRulesSettings.CreateDefaults());
            var sim = new Simulation();
            sim.Reset(config);
            for (int i = 0; i < 60; i++)
            {
                sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            }

            Assert.That(sim.Hash(), Is.EqualTo(run.Hash));

            // Second complete does not duplicate token; flag stays set.
            boot.CompleteCustom(run.Result, request);
            Assert.That(boot.Profile.Ledger.Count, Is.EqualTo(1));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void CrateEligibility_Always_plus_owned_only()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var profile = PlayerProfile.CreateNew();

        IReadOnlyList<int> alwaysOnly = CrateEligibilityQuery.EligibleItemIds(profile, catalog);
        Assert.That(alwaysOnly.Count, Is.GreaterThan(0));
        foreach (int id in alwaysOnly)
        {
            Assert.That(catalog.TryGetItem(id, out ItemRecord item), Is.True);
            Assert.That(item.CrateEligibility, Is.EqualTo(CrateEligibility.Always));
        }

        // Grant a non-Always item if any exist; otherwise grant token (not in catalog — ignored).
        ItemRecord? ownedOnly = null;
        foreach (ItemRecord item in catalog.Items)
        {
            if (item.CrateEligibility == CrateEligibility.WhenOwned)
            {
                ownedOnly = item;
                break;
            }
        }

        if (ownedOnly != null)
        {
            profile.Ledger.Grant(ownedOnly.ItemId);
            IReadOnlyList<int> withOwned = CrateEligibilityQuery.EligibleItemIds(profile, catalog);
            Assert.That(withOwned, Does.Contain(ownedOnly.ItemId));
        }
    }

    [Test]
    public void Menu_shell_exposes_Custom_and_Sparring()
    {
        var menu = new MetaMenuShell();
        var profile = PlayerProfile.CreateNew();
        IReadOnlyList<MetaMenuEntry> entries = menu.BuildEntries(profile);
        Assert.That(entries.Any(e => e.Mode == ModeId.Custom && e.Available), Is.True);
        Assert.That(entries.Any(e => e.Mode == ModeId.Sparring && e.Available), Is.True);
        Assert.That(entries.Any(e => e.Mode == ModeId.Expedition && e.Available), Is.True);

        menu.OpenTinker(profile);
        Assert.That(profile.HasFlag(ProfileFlags.TouchedTinkerStub), Is.True);
    }

    [Test]
    public void Expedition_orchestrator_builds_config_with_gleam_bit()
    {
        var orch = new ExpeditionModeOrchestrator();
        MatchConfig config = orch.BuildConfig(
            PlayerProfile.CreateNew(),
            new ModeRequest { Mode = ModeId.Expedition, Seed = Seed, ActorCount = 2 });
        Assert.That(config.HouseRulesBits & MatchRules.ExpeditionGleams, Is.Not.EqualTo(0));
    }
}
