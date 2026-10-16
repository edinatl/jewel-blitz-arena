using JewelBlitzArena.Meta;

namespace JewelBlitzArena.Tests;

public sealed class TinkerRuntimeTests
{
    [Test]
    public void Outcome_rates_cover_success_double_failure()
    {
        int success = 0, dbl = 0, fail = 0;
        for (ulong seed = 0; seed < 10_000; seed++)
        {
            switch (TinkerRuntime.ResolveOutcome(seed))
            {
                case TinkerOutcome.Success: success++; break;
                case TinkerOutcome.Double: dbl++; break;
                default: fail++; break;
            }
        }

        // Rough band around 55 / 15 / 30 over 10k seeds.
        Assert.That(success, Is.InRange(5200, 5800));
        Assert.That(dbl, Is.InRange(1200, 1800));
        Assert.That(fail, Is.InRange(2700, 3300));
        Assert.That(success + dbl + fail, Is.EqualTo(10_000));
    }

    [Test]
    public void Failure_grants_Scrap_and_Chit()
    {
        var profile = PlayerProfile.CreateNew();
        profile.ProfileSalt = 0x54494E4B455231UL;
        // Find inputs that fail at counter 0.
        bool found = false;
        for (int a = 1; a < 200; a++)
        {
            var probe = PlayerProfile.CreateNew();
            probe.ProfileSalt = profile.ProfileSalt;
            probe.TinkerCounter = 0;
            ulong seed = TinkerRuntime.CraftSeed(probe.ProfileSalt, 0, a, a + 1, 0);
            if (TinkerRuntime.ResolveOutcome(seed) != TinkerOutcome.Failure)
            {
                continue;
            }

            TinkerAttemptResult result = TinkerRuntime.TryCraft(probe, a, a + 1, 0);
            Assert.That(result.Outcome, Is.EqualTo(TinkerOutcome.Failure));
            Assert.That(result.ChitsEarned, Is.EqualTo(1));
            Assert.That(result.ScrapGranted, Is.True);
            Assert.That(probe.Chits, Is.EqualTo(1));
            Assert.That(probe.Ledger.Owns(TinkerRuntime.ScrapItemId), Is.True);
            Assert.That(probe.TinkerCounter, Is.EqualTo(1));
            found = true;
            break;
        }

        Assert.That(found, Is.True);
    }

    [Test]
    public void Same_inputs_same_counter_are_deterministic_after_reload()
    {
        var a = PlayerProfile.CreateNew("Ed");
        a.ProfileSalt = 0xDEADBEEFCAFEUL;
        a.TinkerCounter = 3;
        TinkerAttemptResult first = TinkerRuntime.TryCraft(a, 1001, 1002, 0);

        var b = PlayerProfile.CreateNew("Ed");
        b.ProfileSalt = 0xDEADBEEFCAFEUL;
        b.TinkerCounter = 3;
        TinkerAttemptResult second = TinkerRuntime.TryCraft(b, 1001, 1002, 0);

        Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
        Assert.That(second.ResultItemId, Is.EqualTo(first.ResultItemId));
        Assert.That(second.ChitsEarned, Is.EqualTo(first.ChitsEarned));
        Assert.That(a.TinkerCounter, Is.EqualTo(4));
        Assert.That(b.TinkerCounter, Is.EqualTo(4));
    }

    [Test]
    public void Counter_advances_so_retry_cannot_reroll()
    {
        var profile = PlayerProfile.CreateNew();
        profile.ProfileSalt = 0x5245524F4C4CUL;
        TinkerAttemptResult first = TinkerRuntime.TryCraft(profile, 7, 9, 1);
        int counterAfter = profile.TinkerCounter;
        TinkerAttemptResult second = TinkerRuntime.TryCraft(profile, 7, 9, 1);
        Assert.That(profile.TinkerCounter, Is.EqualTo(counterAfter + 1));
        // Different counter → different craft seed; outcomes may still collide but seeds differ.
        ulong s0 = TinkerRuntime.CraftSeed(profile.ProfileSalt, first.CounterUsed, 7, 9, 1);
        ulong s1 = TinkerRuntime.CraftSeed(profile.ProfileSalt, second.CounterUsed, 7, 9, 1);
        Assert.That(s1, Is.Not.EqualTo(s0));
    }

    [Test]
    public void Three_failures_earn_Facet_Wheel_spin()
    {
        var profile = PlayerProfile.CreateNew();
        profile.ProfileSalt = 0x4348495453504EUL;
        for (int i = 0; i < 200 && profile.Chits < FacetWheelRuntime.SpinCostChits; i++)
        {
            TinkerRuntime.TryCraft(profile, 10 + i, 20 + i, 0);
        }

        Assert.That(profile.Chits, Is.GreaterThanOrEqualTo(FacetWheelRuntime.SpinCostChits));
        Assert.That(
            FacetWheelRuntime.TrySpin(profile, 0x5350494EUL, out _, out _),
            Is.True);
    }

    [Test]
    public void Bench_stub_CanCraft_and_Open_sets_salt()
    {
        var bench = new TinkerModeStub();
        Assert.That(bench.CanCraft, Is.True);
        var profile = PlayerProfile.CreateNew("Rookie");
        bench.Open(profile);
        Assert.That(profile.ProfileSalt, Is.Not.EqualTo(0UL));
        Assert.That(profile.HasFlag(ProfileFlags.TouchedTinkerStub), Is.True);

        TinkerAttemptResult r = bench.Craft(profile, 1, 2, 0);
        Assert.That(r.CounterUsed, Is.EqualTo(0));
        Assert.That(profile.TinkerCounter, Is.EqualTo(1));
    }

    [Test]
    public void Profile_v5_round_trips_salt_and_counter()
    {
        string dir = Path.Combine(Path.GetTempPath(), "jba-tinker-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "profile.jbp");
        try
        {
            var store = new ProfileStore(path);
            PlayerProfile profile = PlayerProfile.CreateNew("Ed");
            profile.ProfileSalt = 0xABCD1234UL;
            profile.TinkerCounter = 42;
            store.Save(profile);

            PlayerProfile loaded = store.LoadOrCreate();
            Assert.That(loaded.SchemaVersion, Is.EqualTo(ProfileSchema.Current));
            Assert.That(loaded.ProfileSalt, Is.EqualTo(0xABCD1234UL));
            Assert.That(loaded.TinkerCounter, Is.EqualTo(42));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }
}
