using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class DataPipelineTests
{
    private const ulong Seed = 0x53355F444154415UL; // S5_DATA-ish

    [Test]
    public void S5_stub_pack_validates_and_has_25_always_items()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        ValidationResult result = ContentValidator.Validate(pack);
        Assert.That(result.IsOk, Is.True, string.Join("; ", result.Errors));
        Assert.That(pack.Items.Count, Is.EqualTo(25));
        Assert.That(pack.Fighters.Count, Is.EqualTo(14));
        Assert.That(pack.Stages.Count, Is.EqualTo(7));
        Assert.That(pack.Stages[0].Phases.Count, Is.EqualTo(3));
        Assert.That(pack.Stages[1].Name, Is.EqualTo("Fulgur Cloister"));
        Assert.That(pack.Stages[1].Phases.Count, Is.EqualTo(2));
        Assert.That(pack.Stages[2].Name, Is.EqualTo("Sunken Strongroom"));
        Assert.That(pack.Stages[2].Phases.Count, Is.EqualTo(3));
        Assert.That(pack.Stages[3].Name, Is.EqualTo("Rime Cutworks"));
        Assert.That(pack.Stages[3].Phases.Count, Is.EqualTo(3));
        Assert.That(pack.Stages[4].Name, Is.EqualTo("The Orrery"));
        Assert.That(pack.Stages[4].Phases.Count, Is.EqualTo(3));
        Assert.That(pack.Stages[5].Name, Is.EqualTo("Terraces — The Harvester"));
        Assert.That(pack.Stages[5].JewelCapOverride, Is.EqualTo(6));
        Assert.That(pack.Stages[6].Name, Is.EqualTo("The Pressworks"));
        Assert.That(pack.Stages[6].Phases[0].BlitzSuppressed, Is.True);
    }

    [Test]
    public void Export_round_trip_preserves_catalog_and_hash()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        ContentCatalog compiled = ContentCompiler.Compile(pack);
        byte[] blob = ContentBlobWriter.Write(compiled);
        ContentCatalog loaded = ContentBlobReader.Read(blob);

        Assert.That(loaded.ContentHash, Is.EqualTo(compiled.ContentHash));
        Assert.That(loaded.Fighters.Length, Is.EqualTo(14));
        Assert.That(loaded.Fighters[0].Name, Is.EqualTo("Rook Citrine"));
        Assert.That(loaded.Fighters[0].MaxLife, Is.EqualTo(1000));
        Assert.That(loaded.Fighters[0].WalkSpeedRaw, Is.EqualTo(Fix.FromInt(3).Raw));
        Assert.That(loaded.Fighters[0].KitBehavior, Is.EqualTo((byte)KitBehavior.None));
        Assert.That(loaded.Fighters[1].Name, Is.EqualTo("Sable"));
        Assert.That(loaded.Fighters[1].KitBehavior, Is.EqualTo((byte)KitBehavior.VanishStrong));
        Assert.That(loaded.Fighters[13].Name, Is.EqualTo("Tilly Setter"));
        Assert.That(loaded.Fighters[13].KitBehavior, Is.EqualTo((byte)KitBehavior.InvoiceRepeatable));
        Assert.That(loaded.Stages[0].Name, Is.EqualTo("Citrine Bazaar"));
        Assert.That(loaded.Stages[0].Phases[0].DurationFrames, Is.EqualTo(25 * 60));
        Assert.That(loaded.Stages[0].Phases[1].DurationFrames, Is.EqualTo(25 * 60));
        Assert.That(loaded.Stages[0].Phases[2].DurationFrames, Is.EqualTo(0));
        Assert.That(loaded.Stages.Length, Is.EqualTo(7));
        Assert.That(loaded.Stages[1].Name, Is.EqualTo("Fulgur Cloister"));
        Assert.That(loaded.Stages[1].Phases[0].DurationFrames, Is.EqualTo(40 * 60));
        Assert.That(loaded.Stages[1].Phases[1].DurationFrames, Is.EqualTo(0));
        Assert.That(loaded.Stages[1].Phases[1].TransitionFrames, Is.EqualTo(4 * 60));
        Assert.That(loaded.Stages[1].Phases[1].CarryItems, Is.EqualTo(CarryItemsMode.HeldOnly));
        Assert.That(loaded.Stages[2].Name, Is.EqualTo("Sunken Strongroom"));
        Assert.That(loaded.Stages[2].Phases[0].DurationFrames, Is.EqualTo(25 * 60));
        Assert.That(loaded.Stages[2].Phases[1].DurationFrames, Is.EqualTo(25 * 60));
        Assert.That(loaded.Stages[2].Phases[2].DurationFrames, Is.EqualTo(0));
        Assert.That(loaded.Stages[2].Phases[1].Hazards[0].Damage, Is.EqualTo(120));
        Assert.That(loaded.Stages[3].Name, Is.EqualTo("Rime Cutworks"));
        Assert.That(loaded.Stages[3].Phases[2].Name, Is.EqualTo("Crane yard"));
        Assert.That(loaded.Stages[3].Phases[2].Kinematics.Length, Is.EqualTo(1));
        Assert.That(loaded.Stages[4].Name, Is.EqualTo("The Orrery"));
        Assert.That(loaded.Stages[4].Phases[1].DurationFrames, Is.EqualTo(30 * 60));
        Assert.That(loaded.Stages[4].Phases[2].Name, Is.EqualTo("Planet disc"));
        Assert.That(loaded.Stages[4].Phases[2].Hazards.Length, Is.EqualTo(2));
        Assert.That(loaded.Stages[5].JewelCapOverride, Is.EqualTo(6));
        Assert.That(loaded.Stages[5].Phases[0].Hazards.Length, Is.EqualTo(4));
        Assert.That(loaded.Stages[6].Phases[0].BlitzSuppressed, Is.True);
        Assert.That(loaded.Stages[6].Phases[1].Name, Is.EqualTo("The Press"));
        Assert.That(loaded.AlwaysEligibleItems().Count, Is.EqualTo(25));
    }

    [Test]
    public void Authored_json_round_trip_compiles_identically()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        string json = AuthoredJson.Serialize(pack);
        AuthoredContentPack again = AuthoredJson.Deserialize(json);
        ContentCatalog a = ContentCompiler.Compile(pack);
        ContentCatalog b = ContentCompiler.Compile(again);
        Assert.That(b.ContentHash, Is.EqualTo(a.ContentHash));
    }

    [Test]
    public void Decimal_fix_parser_does_not_need_float()
    {
        Assert.That(DecimalFixParser.ParseToRaw("3"), Is.EqualTo(3L * DecimalFixParser.OneRaw));
        Assert.That(DecimalFixParser.ParseToRaw("0.5"), Is.EqualTo(DecimalFixParser.OneRaw / 2));
        Assert.That(DecimalFixParser.ParseToRaw("-1.25"), Is.EqualTo(-(DecimalFixParser.OneRaw + DecimalFixParser.OneRaw / 4)));
        Assert.That(DecimalFixParser.ParseToRaw("25"), Is.EqualTo(25L * DecimalFixParser.OneRaw));
    }

    [Test]
    public void Validator_rejects_out_of_band_attack_damage()
    {
        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        pack.Fighters[0].Moves[0].Damage = 99; // Attack must be 30–40
        ValidationResult result = ContentValidator.Validate(pack);
        Assert.That(result.IsOk, Is.False);
        Assert.That(result.Errors.Exists(e => e.Code == "damage_budget"), Is.True);
    }

    [Test]
    public void Simulation_resolves_Rook_life_from_catalog()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());

        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.StartingLife[0] = 0;
            config.StartingLife[1] = 0;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        Assert.That(sim.State.Actor0.Life, Is.EqualTo(1000));
        Assert.That(sim.State.Actor1.Life, Is.EqualTo(1000));
        Assert.That(sim.State.Actor0.FighterId, Is.EqualTo(SampleContentFactory.RookFighterId));
        Assert.That(sim.State.StageId, Is.EqualTo(SampleContentFactory.CitrineBazaarStageId));

        // Still hash-stable with catalog-backed reset.
        ulong hashA = RunHash(config, catalog);
        ulong hashB = RunHash(config, catalog);
        Assert.That(hashA, Is.EqualTo(hashB));
    }

    [Test]
    public void Golden_blob_on_disk_matches_factory_when_present()
    {
        string root = FindRepoRoot();
        string blobPath = Path.Combine(root, "content", "export", "s5_stub.jba");
        if (!File.Exists(blobPath))
        {
            Assert.Ignore("Golden blob not generated yet; export tool creates content/export/s5_stub.jba");
        }

        ContentCatalog expected = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        ContentCatalog disk = ContentBlobReader.Read(File.ReadAllBytes(blobPath));
        Assert.That(disk.ContentHash, Is.EqualTo(expected.ContentHash));
    }

    private static ulong RunHash(MatchConfig config, ContentCatalog catalog)
    {
        var sim = new Simulation();
        sim.Reset(config, catalog);
        for (int f = 0; f < 60; f++)
        {
            sim.Step(
                InputFrame.FromStick(f % 32, 2),
                InputFrame.FromStick((f * 3) % 32, 1),
                InputFrame.Neutral,
                InputFrame.Neutral);
        }

        return sim.Hash();
    }

    private static string FindRepoRoot()
    {
        string dir = TestContext.CurrentContext.TestDirectory;
        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "JewelBlitzArena.sln")))
            {
                return dir;
            }

            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
