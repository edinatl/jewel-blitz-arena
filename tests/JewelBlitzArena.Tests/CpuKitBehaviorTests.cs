using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class CpuKitBehaviorTests
{
    [Test]
    public void Kit_behavior_parses_and_compiles_onto_actor()
    {
        Assert.That(ContentValidator.TryParseKitBehavior("VanishStrong", out byte vanish), Is.True);
        Assert.That(vanish, Is.EqualTo((byte)KitBehavior.VanishStrong));
        Assert.That(ContentValidator.TryParseKitBehavior("riposte_strong", out byte riposte), Is.True);
        Assert.That(riposte, Is.EqualTo((byte)KitBehavior.RiposteStrong));
        Assert.That(ContentValidator.TryParseKitBehavior("not-a-behavior", out _), Is.False);

        AuthoredContentPack pack = SampleContentFactory.CreateS5StubPack();
        pack.Fighters[0].KitBehavior = "InvoiceRepeatable";
        ContentCatalog catalog = ContentCompiler.Compile(pack);
        Assert.That(catalog.Fighters[0].KitBehavior, Is.EqualTo((byte)KitBehavior.InvoiceRepeatable));

        MatchConfig config = MatchConfig.CreateDefault(0x4B4954423148UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.Actor0.KitBehavior, Is.EqualTo((byte)KitBehavior.InvoiceRepeatable));
        Assert.That(sim.State.Actor1.KitBehavior, Is.EqualTo((byte)KitBehavior.InvoiceRepeatable));
    }

    [Test]
    public void VanishStrong_cpu_prefers_Strong_when_in_mid_range()
    {
        InputFrame frame = RunCpuNearFoe(KitBehavior.VanishStrong, level: 7, foeOffset: Fix.FromInt(2));
        Assert.That(frame.Strong || frame.Attack, Is.True);
    }

    [Test]
    public void CommandGrab_cpu_presses_Action_when_empty_handed_in_reach()
    {
        InputFrame frame = RunCpuNearFoe(KitBehavior.CommandGrabCarry, level: 6, foeOffset: Fix.FromInt(1));
        Assert.That(frame.Action, Is.True);
    }

    [Test]
    public void Riposte_cpu_retreats_when_foe_is_far()
    {
        // Place foe far on +X; riposte should retreat (−X), not chase (+X).
        InputFrame frame = RunCpuNearFoe(KitBehavior.RiposteStrong, level: 5, foeOffset: Fix.FromInt(12));
        // Direction 16 is −X in DirectionLut (index 0 = +X, 16 = −X).
        Assert.That(frame.Direction, Is.EqualTo(16).Or.EqualTo(15).Or.EqualTo(17));
        Assert.That(frame.Magnitude, Is.GreaterThan(0));
    }

    [Test]
    public void SwapPartners_and_Invoice_are_hash_stable()
    {
        AssertHashStable(KitBehavior.SwapPartners, 0x5357415031UL);
        AssertHashStable(KitBehavior.InvoiceRepeatable, 0x494E563031UL);
        AssertHashStable(KitBehavior.TripleJumpGlide, 0x4A4D503031UL);
        AssertHashStable(KitBehavior.ExtendedDodge, 0x4444474531UL);
        AssertHashStable(KitBehavior.DoubleHold, 0x44424C4831UL);
    }

    [Test]
    public void Different_kit_behaviors_diverge_from_same_seed()
    {
        ulong seed = 0x4449564552UL;
        ulong hNone = RunHash(KitBehavior.None, seed);
        ulong hInvoice = RunHash(KitBehavior.InvoiceRepeatable, seed);
        ulong hStrafe = RunHash(KitBehavior.SwapPartners, seed);
        Assert.That(hInvoice, Is.Not.EqualTo(hNone));
        Assert.That(hStrafe, Is.Not.EqualTo(hNone));
        Assert.That(hStrafe, Is.Not.EqualTo(hInvoice));
    }

    [Test]
    public void Stub_cast_fighters_compile_named_kit_behaviors()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.Fighters.Length, Is.EqualTo(14));

        AssertBehavior(catalog, SampleContentFactory.RookFighterId, KitBehavior.None);
        AssertBehavior(catalog, SampleContentFactory.SableFighterId, KitBehavior.VanishStrong);
        AssertBehavior(catalog, SampleContentFactory.BulwarkFighterId, KitBehavior.CommandGrabCarry);
        AssertBehavior(catalog, SampleContentFactory.LuneFighterId, KitBehavior.RiposteStrong);
        AssertBehavior(catalog, SampleContentFactory.TeoFighterId, KitBehavior.ExtendedDodge);
        AssertBehavior(catalog, SampleContentFactory.WrenFighterId, KitBehavior.TripleJumpGlide);
        AssertBehavior(catalog, SampleContentFactory.ClankFighterId, KitBehavior.DoubleHold);
        AssertBehavior(catalog, SampleContentFactory.PipPepperFighterId, KitBehavior.SwapPartners);
        AssertBehavior(catalog, SampleContentFactory.TillyFighterId, KitBehavior.InvoiceRepeatable);

        MatchConfig config = MatchConfig.CreateDefault(0x434153543148UL, actorCount: 2);
        unsafe
        {
            config.FighterIds[0] = SampleContentFactory.SableFighterId;
            config.FighterIds[1] = SampleContentFactory.TillyFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.Actor0.KitBehavior, Is.EqualTo((byte)KitBehavior.VanishStrong));
        Assert.That(sim.State.Actor1.KitBehavior, Is.EqualTo((byte)KitBehavior.InvoiceRepeatable));
        Assert.That(sim.State.Actor0.Life, Is.EqualTo(850));
        Assert.That(sim.State.Actor1.Life, Is.EqualTo(850));
    }

    [Test]
    public void Blitzed_cpu_may_press_Surge_or_Flare_Attack()
    {
        MatchConfig config = MatchConfig.CreateDefault(0x424C545A31UL, actorCount: 2);
        config.SetCpuLevel(1, 8);
        var sim = new Simulation();
        sim.Reset(config);
        MatchState snap = sim.Snapshot();
        snap.Actor1.KitBehavior = (byte)KitBehavior.InvoiceRepeatable;
        snap.Actor1.ReachRating = 3;
        snap.Actor1.PosX = Fix.Zero;
        snap.Actor1.PosZ = Fix.Zero;
        snap.Actor0.PosX = Fix.FromInt(1);
        snap.Actor0.PosZ = Fix.Zero;
        snap.Actor1.IsBlitzed = true;
        snap.Actor1.Luster = MatchRules.LusterMax;
        snap.Actor1.TransformFrames = 0;
        sim.Restore(snap);
        sim.SetCpuControlled(1, true, 8);

        bool sawSurgeOrFlare = false;
        for (int i = 0; i < 64; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            MatchState after = sim.Snapshot();
            if (!after.Actor1.IsBlitzed)
            {
                break;
            }

            var frame = new InputFrame
            {
                Bits = (ushort)MatchStateAccess.GetCpuBrain(ref after, 1).LastDecision,
            };
            if (frame.SurgeA || frame.SurgeB || frame.Attack)
            {
                sawSurgeOrFlare = true;
                break;
            }
        }

        Assert.That(sawSurgeOrFlare, Is.True);
    }

    private static void AssertBehavior(ContentCatalog catalog, int fighterId, KitBehavior expected)
    {
        Assert.That(catalog.TryGetFighter(fighterId, out FighterKitRecord kit), Is.True);
        Assert.That(kit.KitBehavior, Is.EqualTo((byte)expected), kit.Name);
    }

    private static InputFrame RunCpuNearFoe(KitBehavior behavior, int level, Fix foeOffset)
    {
        MatchConfig config = MatchConfig.CreateDefault(0x4E45415231UL, actorCount: 2);
        config.SetCpuLevel(1, level);
        var sim = new Simulation();
        sim.Reset(config);
        MatchState snap = sim.Snapshot();
        snap.Actor1.KitBehavior = (byte)behavior;
        snap.Actor1.ReachRating = 3;
        snap.Actor1.PosX = Fix.Zero;
        snap.Actor1.PosZ = Fix.Zero;
        snap.Actor0.PosX = foeOffset;
        snap.Actor0.PosZ = Fix.Zero;
        sim.Restore(snap);
        sim.SetCpuControlled(1, true, level);

        // Aggregate button bits across thinks (personalities roll RNG); keep last stick.
        InputFrame last = InputFrame.Neutral;
        bool attack = false, strong = false, jump = false, action = false;
        for (int i = 0; i < 48; i++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
            MatchState after = sim.Snapshot();
            CpuBrainSlot brain = MatchStateAccess.GetCpuBrain(ref after, 1);
            last = new InputFrame { Bits = (ushort)brain.LastDecision };
            attack |= last.Attack;
            strong |= last.Strong;
            jump |= last.Jump;
            action |= last.Action;
        }

        last.Attack = attack;
        last.Strong = strong;
        last.Jump = jump;
        last.Action = action;
        return last;
    }

    private static void AssertHashStable(KitBehavior behavior, ulong seed)
    {
        ulong a = RunHash(behavior, seed);
        ulong b = RunHash(behavior, seed);
        Assert.That(a, Is.EqualTo(b));
    }

    private static ulong RunHash(KitBehavior behavior, ulong seed)
    {
        MatchConfig config = MatchConfig.CreateDefault(seed, actorCount: 2);
        config.SetCpuLevel(1, 5);
        var sim = new Simulation();
        sim.Reset(config);
        MatchState snap = sim.Snapshot();
        snap.Actor1.KitBehavior = (byte)behavior;
        sim.Restore(snap);
        sim.SetCpuControlled(1, true);

        InputFrame p0 = InputFrame.FromStick(0, 2);
        for (int i = 0; i < 75; i++)
        {
            sim.Step(p0, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        return sim.Hash();
    }
}
