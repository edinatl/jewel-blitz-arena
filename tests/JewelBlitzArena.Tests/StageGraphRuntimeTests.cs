using JewelBlitzArena.Data;
using JewelBlitzArena.Sim;

namespace JewelBlitzArena.Tests;

public sealed class StageGraphRuntimeTests
{
    private const ulong Seed = 0x53345F5354414745UL; // S4_STAGE

    [Test]
    public void Citrine_Bazaar_compiles_three_phases_with_breakaway_rules()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetStage(SampleContentFactory.CitrineBazaarStageId, out StageRecord stage), Is.True);
        Assert.That(stage.Phases.Length, Is.EqualTo(3));
        Assert.That(stage.Phases[0].DurationFrames, Is.EqualTo(25 * 60));
        Assert.That(stage.Phases[1].Name, Is.EqualTo("Breakaway"));
        Assert.That(stage.Phases[1].CarryJewels, Is.EqualTo(CarryJewelsMode.DropChance));
        Assert.That(stage.Phases[1].CarryJewelsDropPermille, Is.EqualTo(500));
        Assert.That(stage.Phases[1].TransitionFrames, Is.EqualTo(2 * 60));
        Assert.That(stage.Phases[1].Kinematics.Length, Is.EqualTo(1));
        Assert.That(stage.Phases[2].DurationFrames, Is.EqualTo(0));
        Assert.That(stage.Phases[0].CameraRadiusRaw, Is.EqualTo(Fix.FromInt(12).Raw));
    }

    [Test]
    public void Phase_timers_advance_Bazaar_to_Breakaway_to_Geode()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(0));
        Assert.That(sim.State.ActiveGeometrySetId, Is.EqualTo(0));
        Assert.That(sim.State.PhaseDurationFrames, Is.EqualTo(25 * 60));

        // Run exactly through Bazaar duration.
        for (int f = 0; f < 25 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        // Enter Breakaway transition (2 s) — still phase index 1, in transition.
        Assert.That(sim.State.PhaseIndex, Is.EqualTo(1));
        Assert.That((sim.State.StageFlags & StageFlags.InTransition) != 0, Is.True);
        Assert.That(sim.State.ActiveGeometrySetId, Is.EqualTo(1));

        for (int f = 0; f < 2 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That((sim.State.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That((sim.State.StageFlags & StageFlags.HazardsEnabled) != 0, Is.True);
        Assert.That(sim.State.ActiveKinematicCount, Is.EqualTo(1));

        // Finish Breakaway (25 s phase clock after transition completed — elapsed during transition?).
        // PhaseElapsedFrames only ticks outside transition, so need 25*60 more steps.
        for (int f = 0; f < 25 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(2));
        Assert.That(sim.State.ActiveGeometrySetId, Is.EqualTo(2));
        Assert.That(sim.State.PhaseDurationFrames, Is.EqualTo(0));
    }

    [Test]
    public void Phase_clock_permille_scales_durations()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 1UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.PhaseClockPermille = 500; // 50% clock → 12.5 s bazaar
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.PhaseDurationFrames, Is.EqualTo(25 * 60 / 2));

        for (int f = 0; f < 25 * 60 / 2; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(1));
    }

    [Test]
    public void Breakaway_drop_chance_is_hash_stable()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 2UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.CitrineBazaarStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        ulong a = RunThroughBreakaway(config, catalog, grantJewels: true);
        ulong b = RunThroughBreakaway(config, catalog, grantJewels: true);
        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Kinematic_triangle_offset_is_deterministic()
    {
        Fix origin = Fix.FromInt(0);
        Fix amp = Fix.FromInt(2);
        Fix peak = StageRuntime.TriangleOffset(origin, amp, frame: 240, periodFrames: 480);
        Fix peak2 = StageRuntime.TriangleOffset(origin, amp, frame: 240, periodFrames: 480);
        Assert.That(peak.Raw, Is.EqualTo(peak2.Raw));
        Assert.That(peak.Raw, Is.EqualTo(amp.Raw));
        Assert.That(StageRuntime.TriangleOffset(origin, amp, 0, 480).Raw, Is.EqualTo((-amp).Raw));
    }

    [Test]
    public void Flat_greybox_without_catalog_stays_phase_zero()
    {
        var config = MatchConfig.CreateDefault(Seed ^ 3UL, actorCount: 2);
        var sim = new Simulation();
        sim.Reset(config);
        for (int f = 0; f < 300; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(0));
        Assert.That(sim.StageGraph.Phases.Length, Is.EqualTo(0));
    }

    [Test]
    public void Fulgur_Cloister_compiles_courtyard_library_with_held_only()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetStage(SampleContentFactory.FulgurCloisterStageId, out StageRecord stage), Is.True);
        Assert.That(stage.Phases.Length, Is.EqualTo(2));
        Assert.That(stage.Phases[0].Name, Is.EqualTo("Courtyard"));
        Assert.That(stage.Phases[0].DurationFrames, Is.EqualTo(40 * 60));
        Assert.That(stage.Phases[1].Name, Is.EqualTo("Library"));
        Assert.That(stage.Phases[1].TransitionFrames, Is.EqualTo(4 * 60));
        Assert.That(stage.Phases[1].CarryItems, Is.EqualTo(CarryItemsMode.HeldOnly));
        Assert.That(stage.Phases[1].Kinematics.Length, Is.EqualTo(1));
        Assert.That(stage.Phases[1].Hazards[0].Damage, Is.EqualTo(120));
    }

    [Test]
    public void Fulgur_phase_timers_advance_Courtyard_to_Library_with_held_only()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 4UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.FulgurCloisterStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(0));
        Assert.That(sim.State.ActiveHazardCount, Is.EqualTo(1));
        Assert.That(sim.State.PhaseDurationFrames, Is.EqualTo(40 * 60));

        // Spawn a loose field item; Landslide held_only should despawn it.
        int loose = sim.SpawnLooseItem(MatchRules.AdhesiveSprayItemId, Fix.FromInt(3), Fix.Zero, Fix.Zero);
        Assert.That(loose, Is.GreaterThanOrEqualTo(0));

        for (int f = 0; f < 40 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState landslide = sim.Snapshot();
        Assert.That(landslide.PhaseIndex, Is.EqualTo(1));
        Assert.That((landslide.StageFlags & StageFlags.InTransition) != 0, Is.True);
        Assert.That(landslide.ActiveHazardCount, Is.EqualTo(0));
        LooseItemSlot field = MatchStateAccess.GetLooseItem(ref landslide, loose);
        Assert.That(field.IsActive, Is.False, "held_only transition despawns field items");

        for (int f = 0; f < 4 * 60; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState library = sim.Snapshot();
        Assert.That((library.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That((library.StageFlags & StageFlags.HazardsEnabled) != 0, Is.True);
        Assert.That(library.ActiveHazardCount, Is.EqualTo(1));
        Assert.That(library.ActiveKinematicCount, Is.EqualTo(1));
        Assert.That(library.PhaseDurationFrames, Is.EqualTo(0));
    }

    [Test]
    public void Schema_v2_blob_round_trip_preserves_hazards()
    {
        ContentCatalog compiled = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        byte[] blob = ContentBlobWriter.Write(compiled);
        ContentCatalog loaded = ContentBlobReader.Read(blob);
        Assert.That(loaded.ContentHash, Is.EqualTo(compiled.ContentHash));
        Assert.That(loaded.Stages[0].Phases[1].Hazards.Length, Is.EqualTo(1));
        Assert.That(loaded.Stages[0].Phases[1].Hazards[0].Damage, Is.EqualTo(80));
        Assert.That(loaded.SchemaVersion, Is.EqualTo(ContentSchema.Version));
    }

    [Test]
    public void Sunken_Strongroom_compiles_three_phases_with_rolling_door()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(
            catalog.TryGetStage(SampleContentFactory.SunkenStrongroomStageId, out StageRecord stage),
            Is.True);
        Assert.That(stage.Phases.Length, Is.EqualTo(3));
        Assert.That(stage.Phases[0].Name, Is.EqualTo("Vault floor"));
        Assert.That(stage.Phases[1].Name, Is.EqualTo("Vault door"));
        Assert.That(stage.Phases[1].Kinematics.Length, Is.EqualTo(1));
        Assert.That(stage.Phases[1].Hazards[0].Damage, Is.EqualTo(120));
        Assert.That(stage.Phases[2].Name, Is.EqualTo("Hull breach"));
        Assert.That(stage.Phases[2].Hazards[0].Damage, Is.EqualTo(150));
        Assert.That(stage.Phases[2].DurationFrames, Is.EqualTo(0));
    }

    [Test]
    public void Sunken_phase_timers_reach_Hull_breach()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 5UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.SunkenStrongroomStageId;
            config.PhaseClockPermille = 100; // 10% → 150f vault phases
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.PhaseIndex, Is.EqualTo(0));

        // Vault floor 150f + transition 12f → Vault door.
        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(1));
        Assert.That((sim.State.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That(sim.State.ActiveKinematicCount, Is.EqualTo(1));

        // Vault door 150f + transition 12f → Hull breach.
        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState breach = sim.Snapshot();
        Assert.That(breach.PhaseIndex, Is.EqualTo(2));
        Assert.That((breach.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That(breach.ActiveHazardCount, Is.EqualTo(1));
        Assert.That(breach.PhaseDurationFrames, Is.EqualTo(0));
    }

    [Test]
    public void Rime_Cutworks_compiles_three_phases_with_crane()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(
            catalog.TryGetStage(SampleContentFactory.RimeCutworksStageId, out StageRecord stage),
            Is.True);
        Assert.That(stage.Phases.Length, Is.EqualTo(3));
        Assert.That(stage.Phases[0].Name, Is.EqualTo("Dry yard"));
        Assert.That(stage.Phases[0].Kinematics.Length, Is.EqualTo(1));
        Assert.That(stage.Phases[1].Name, Is.EqualTo("Flooding"));
        Assert.That(stage.Phases[2].Name, Is.EqualTo("Crane yard"));
        Assert.That(stage.Phases[2].Hazards[0].Damage, Is.EqualTo(100));
        Assert.That(stage.Phases[2].Kinematics[0].TrackId, Is.EqualTo(32));
    }

    [Test]
    public void Rime_phase_timers_reach_Crane_yard()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 6UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.RimeCutworksStageId;
            config.PhaseClockPermille = 100;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.PhaseIndex, Is.EqualTo(0));
        Assert.That(sim.State.ActiveKinematicCount, Is.EqualTo(1));

        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(1));
        Assert.That(sim.State.ActiveKinematicCount, Is.EqualTo(1));

        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState crane = sim.Snapshot();
        Assert.That(crane.PhaseIndex, Is.EqualTo(2));
        Assert.That((crane.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That(crane.ActiveHazardCount, Is.EqualTo(1));
        Assert.That(crane.ActiveKinematicCount, Is.EqualTo(1));
        Assert.That(crane.PhaseDurationFrames, Is.EqualTo(0));
    }

    [Test]
    public void Orrery_compiles_three_phases_with_beetle_and_arm_gap()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(catalog.TryGetStage(SampleContentFactory.OrreryStageId, out StageRecord stage), Is.True);
        Assert.That(stage.Phases.Length, Is.EqualTo(3));
        Assert.That(stage.Phases[0].Name, Is.EqualTo("Gear floor"));
        Assert.That(stage.Phases[0].Kinematics.Length, Is.EqualTo(1));
        Assert.That(stage.Phases[1].Name, Is.EqualTo("Infestation"));
        Assert.That(stage.Phases[1].DurationFrames, Is.EqualTo(30 * 60));
        Assert.That(stage.Phases[1].Hazards.Length, Is.EqualTo(2));
        Assert.That(stage.Phases[2].Name, Is.EqualTo("Planet disc"));
        Assert.That(stage.Phases[2].Hazards[0].Damage, Is.EqualTo(150));
        Assert.That(stage.Phases[2].Kinematics.Length, Is.EqualTo(1));
    }

    [Test]
    public void Orrery_phase_timers_reach_Planet_disc()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 7UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.OrreryStageId;
            config.PhaseClockPermille = 100; // Gear 150f + Infestation 180f → Planet
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.PhaseIndex, Is.EqualTo(0));
        Assert.That(sim.State.ActiveKinematicCount, Is.EqualTo(1));

        // Gear floor 150f, transition 0 → Infestation.
        for (int f = 0; f < 150; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        Assert.That(sim.State.PhaseIndex, Is.EqualTo(1));
        Assert.That(sim.State.ActiveHazardCount, Is.EqualTo(2));

        // Infestation 180f + Planet transition 12f → Planet disc.
        for (int f = 0; f < 180 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState disc = sim.Snapshot();
        Assert.That(disc.PhaseIndex, Is.EqualTo(2));
        Assert.That((disc.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That(disc.ActiveHazardCount, Is.EqualTo(2));
        Assert.That(disc.ActiveKinematicCount, Is.EqualTo(1));
        Assert.That(disc.PhaseDurationFrames, Is.EqualTo(0));
    }

    [Test]
    public void Harvester_compiles_open_terrace_with_jewel_cap_6()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(
            catalog.TryGetStage(SampleContentFactory.HarvesterTerracesStageId, out StageRecord stage),
            Is.True);
        Assert.That(stage.JewelCapOverride, Is.EqualTo(6));
        Assert.That(stage.Phases.Length, Is.EqualTo(1));
        Assert.That(stage.Phases[0].Name, Is.EqualTo("Terrace hunt"));
        Assert.That(stage.Phases[0].DurationFrames, Is.EqualTo(0));
        Assert.That(stage.Phases[0].Hazards.Length, Is.EqualTo(4));
        Assert.That(stage.Phases[0].Kinematics.Length, Is.EqualTo(1));

        var config = MatchConfig.CreateDefault(Seed ^ 8UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.JewelCap, Is.EqualTo(6));
        Assert.That(sim.State.ActiveHazardCount, Is.EqualTo(4));
        Assert.That(sim.State.ActiveKinematicCount, Is.EqualTo(1));
    }

    [Test]
    public void Pressworks_Sorting_is_Blitz_suppressed_then_Press()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        Assert.That(
            catalog.TryGetStage(SampleContentFactory.PressworksStageId, out StageRecord stage),
            Is.True);
        Assert.That(stage.JewelCapOverride, Is.EqualTo(6));
        Assert.That(stage.Phases[0].BlitzSuppressed, Is.True);
        Assert.That(stage.Phases[1].Name, Is.EqualTo("The Press"));

        var config = MatchConfig.CreateDefault(Seed ^ 9UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.PressworksStageId;
            config.PhaseClockPermille = 100;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);
        Assert.That(sim.State.JewelCap, Is.EqualTo(6));
        Assert.That((sim.State.StageFlags & StageFlags.BlitzSuppressed) != 0, Is.True);

        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState press = sim.Snapshot();
        Assert.That(press.PhaseIndex, Is.EqualTo(1));
        Assert.That((press.StageFlags & StageFlags.InTransition) == 0, Is.True);
        Assert.That((press.StageFlags & StageFlags.BlitzSuppressed) == 0, Is.True);
        Assert.That(press.ActiveHazardCount, Is.EqualTo(3));
        Assert.That(press.ActiveKinematicCount, Is.EqualTo(1));
    }

    [Test]
    public void Harvester_footfall_tracks_patrol_kinematic()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 10UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        // Advance past first kinematic step so patrol leaves origin.
        for (int f = 0; f < 90; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState snap = sim.Snapshot();
        GenericPoolSlot footfall = default;
        GenericPoolSlot patrol = default;
        bool foundFoot = false;
        bool foundPatrol = false;
        for (int i = 0; i < snap.ActiveHazardCount; i++)
        {
            GenericPoolSlot h = MatchStateAccess.GetHazard(ref snap, i);
            if (h.TypeId == BossAiRuntime.FootfallShadowId)
            {
                footfall = h;
                foundFoot = true;
            }
        }

        for (int i = 0; i < snap.ActiveKinematicCount; i++)
        {
            GenericPoolSlot k = MatchStateAccess.GetKinematic(ref snap, i);
            if (k.TypeId == BossAiRuntime.HarvesterPatrolTrackId)
            {
                patrol = k;
                foundPatrol = true;
            }
        }

        Assert.That(foundFoot && foundPatrol, Is.True);
        Assert.That(footfall.PosX.Raw, Is.EqualTo(patrol.PosX.Raw));
        Assert.That(footfall.PosZ.Raw, Is.EqualTo(patrol.PosZ.Raw));
        Assert.That(patrol.PosX.Raw, Is.Not.EqualTo(0), "patrol should have left origin by frame 90");
    }

    [Test]
    public void Harvester_stomp_applies_dizzy_to_grounded_fighters()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 11UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.HarvesterTerracesStageId;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        // StompShock period = 15 s = 900 f. Advance to the frame before the pulse, then
        // park the fighter at the stomp center so radius 4 covers them.
        for (int f = 0; f < 899; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState parked = sim.Snapshot();
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref parked, 0);
            a0.PosX = Fix.Zero;
            a0.PosY = Fix.Zero;
            a0.PosZ = Fix.Zero;
            a0.DizzyFrames = 0;
            a0.DizzyImmuneFrames = 0;
        }

        sim.Restore(parked);
        sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);

        MatchState after = sim.Snapshot();
        ActorSlot victim = MatchStateAccess.Actor(ref after, 0);
        Assert.That(victim.DizzyFrames, Is.GreaterThan(0));
        Assert.That(victim.DizzyFrames, Is.LessThanOrEqualTo(MatchRules.DizzyDurationFrames));
    }

    [Test]
    public void Pressworks_live_hands_chase_toward_fighter()
    {
        ContentCatalog catalog = ContentCompiler.Compile(SampleContentFactory.CreateS5StubPack());
        var config = MatchConfig.CreateDefault(Seed ^ 12UL, actorCount: 2);
        unsafe
        {
            config.StageId = SampleContentFactory.PressworksStageId;
            config.PhaseClockPermille = 100;
            config.FighterIds[0] = SampleContentFactory.RookFighterId;
            config.FighterIds[1] = SampleContentFactory.RookFighterId;
        }

        var sim = new Simulation();
        sim.Reset(config, catalog);

        // Clear Sorting (150f) + transition (12f) into The Press.
        for (int f = 0; f < 150 + 12; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState press = sim.Snapshot();
        Assert.That(press.PhaseIndex, Is.EqualTo(1));

        long startX = 0;
        bool found = false;
        for (int i = 0; i < press.ActiveHazardCount; i++)
        {
            GenericPoolSlot h = MatchStateAccess.GetHazard(ref press, i);
            if (h.TypeId == BossAiRuntime.LiveHandsChaseId)
            {
                startX = h.PosX.Raw;
                found = true;
            }
        }

        Assert.That(found, Is.True);

        // Park fighters far on +X so chase moves Live Hands rightward.
        {
            ref ActorSlot a0 = ref MatchStateAccess.Actor(ref press, 0);
            a0.PosX = Fix.FromInt(8);
            a0.PosZ = Fix.FromInt(2);
            ref ActorSlot a1 = ref MatchStateAccess.Actor(ref press, 1);
            a1.PosX = Fix.FromInt(8);
            a1.PosZ = Fix.FromInt(2);
        }

        sim.Restore(press);

        for (int f = 0; f < 120; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        MatchState chased = sim.Snapshot();
        long endX = startX;
        for (int i = 0; i < chased.ActiveHazardCount; i++)
        {
            GenericPoolSlot h = MatchStateAccess.GetHazard(ref chased, i);
            if (h.TypeId == BossAiRuntime.LiveHandsChaseId)
            {
                endX = h.PosX.Raw;
            }
        }

        Assert.That(endX, Is.GreaterThan(startX), "Live Hands should advance toward +X fighter");
    }

    private static ulong RunThroughBreakaway(MatchConfig config, ContentCatalog catalog, bool grantJewels)
    {
        var sim = new Simulation();
        sim.Reset(config, catalog);
        if (grantJewels)
        {
            sim.DebugGrantJewels(0, 2);
            sim.DebugGrantJewels(1, 2);
        }

        int frames = (25 * 60) + (2 * 60) + 30;
        for (int f = 0; f < frames; f++)
        {
            sim.Step(InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral, InputFrame.Neutral);
        }

        return sim.Hash();
    }
}
