using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Phase timers, transitions, hazard volumes, and kinematic tracks (architecture §2.7).
    /// Camera volumes stay on the catalog for Presentation.
    /// </summary>
    public static class StageRuntime
    {
        public const byte HazardFlagActive = 1 << 0;
        public const byte HazardFlagTelegraph = 1 << 1;
        public const byte KinematicFlagActive = 1 << 0;

        public static StageGraph ResolveGraph(ContentCatalog? catalog, int stageId)
        {
            if (catalog != null && catalog.TryGetStage(stageId, out StageRecord record))
            {
                return StageGraph.FromRecord(record);
            }

            return StageGraph.Empty;
        }

        public static void InitializeMatch(
            ref MatchState state,
            StageGraph graph,
            int phaseClockPermille,
            ref SimContactBuffer events)
        {
            state.PhaseClockPermille = phaseClockPermille <= 0 ? 1000 : phaseClockPermille;
            state.StageFlags = 0;
            if (graph.FallIsKo)
            {
                state.StageFlags |= StageFlags.FallIsKo;
            }

            ClearHazards(ref state);
            ClearKinematics(ref state);

            if (graph.Phases.Length == 0)
            {
                state.PhaseIndex = 0;
                state.PhaseElapsedFrames = 0;
                state.PhaseDurationFrames = 0;
                state.TransitionRemainingFrames = 0;
                state.ActiveGeometrySetId = 0;
                return;
            }

            EnterPhase(ref state, graph, phaseIndex: 0, applyEnterTransition: false, ref events);
        }

        public static void Tick(
            ref MatchState state,
            StageGraph graph,
            ref SimContactBuffer events)
        {
            if (graph.Phases.Length == 0 || state.MatchOver)
            {
                return;
            }

            if ((state.StageFlags & StageFlags.InTransition) != 0)
            {
                if (state.TransitionRemainingFrames > 0)
                {
                    state.TransitionRemainingFrames--;
                }

                if (state.TransitionRemainingFrames <= 0)
                {
                    state.StageFlags &= ~StageFlags.InTransition;
                    state.StageFlags |= StageFlags.HazardsEnabled;
                    ArmPhaseHazards(ref state, graph);
                    BreakableRuntime.ArmPhaseCrates(ref state, graph);
                    if (graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef arrived))
                    {
                        events.Add(SimEventKind.PhaseChanged, (byte)state.PhaseIndex, (byte)arrived.GeometrySetId);
                    }
                }

                StepKinematics(ref state, graph);
                return;
            }

            // Kinematics first so boss AI can bind hazard centers to patrol tracks.
            StepKinematics(ref state, graph);
            BossAiRuntime.BindCenters(ref state, graph);
            StepHazards(ref state, graph, ref events);

            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            if (phase.DurationFrames <= 0)
            {
                // Final / open-ended phase.
                state.PhaseElapsedFrames++;
                BossTimeoutRuntime.Tick(ref state, ref events);
                return;
            }

            state.PhaseElapsedFrames++;
            BossTimeoutRuntime.Tick(ref state, ref events);

            // Pressworks Sorting: open gate early when enough Vein Guards are down.
            bool gateReady = StageCreatureRuntime.SortingGateReady(ref state, graph);
            if (!gateReady && state.PhaseElapsedFrames < state.PhaseDurationFrames)
            {
                return;
            }

            int next = state.PhaseIndex + 1;
            if (next >= graph.Phases.Length)
            {
                return;
            }

            EnterPhase(ref state, graph, next, applyEnterTransition: true, ref events);
        }

        public static bool IsBlitzSuppressed(in MatchState state) =>
            (state.StageFlags & StageFlags.BlitzSuppressed) != 0;

        /// <summary>Frames until Blade Sweep / Flood (0 if N/A or already fired).</summary>
        public static int BossTimeoutRemaining(in MatchState state) =>
            BossTimeoutRuntime.Remaining(in state);

        private static void EnterPhase(
            ref MatchState state,
            StageGraph graph,
            int phaseIndex,
            bool applyEnterTransition,
            ref SimContactBuffer events)
        {
            if (!graph.TryGetPhase(phaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            state.PhaseIndex = phaseIndex;
            state.PhaseElapsedFrames = 0;
            state.PhaseDurationFrames = ScaleFrames(phase.DurationFrames, state.PhaseClockPermille);
            state.ActiveGeometrySetId = phase.GeometrySetId;

            if (phase.BlitzSuppressed)
            {
                state.StageFlags |= StageFlags.BlitzSuppressed;
            }
            else
            {
                state.StageFlags &= ~StageFlags.BlitzSuppressed;
            }

            ClearHazards(ref state);
            ClearKinematics(ref state);
            ActivateKinematics(ref state, phase);
            StageCreatureRuntime.OnPhaseEnter(ref state, graph, phaseIndex);
            VitreBossRuntime.OnPhaseEnter(ref state, graph, phaseIndex);

            int transition = 0;
            if (applyEnterTransition)
            {
                transition = ScaleFrames(phase.TransitionFrames, state.PhaseClockPermille);
                ApplyCarryRules(ref state, phase);
            }

            if (transition > 0)
            {
                state.TransitionRemainingFrames = transition;
                state.StageFlags |= StageFlags.InTransition;
                state.StageFlags &= ~StageFlags.HazardsEnabled;
                events.Add(SimEventKind.PhaseTransitionStarted, (byte)phaseIndex, (byte)phase.GeometrySetId);
            }
            else
            {
                state.TransitionRemainingFrames = 0;
                state.StageFlags &= ~StageFlags.InTransition;
                state.StageFlags |= StageFlags.HazardsEnabled;
                ArmPhaseHazards(ref state, graph);
                BreakableRuntime.ArmPhaseCrates(ref state, graph);
                events.Add(SimEventKind.PhaseChanged, (byte)phaseIndex, (byte)phase.GeometrySetId);
            }
        }

        private static int ScaleFrames(int authoredFrames, int permille)
        {
            if (authoredFrames <= 0)
            {
                return 0;
            }

            int p = permille <= 0 ? 1000 : permille;
            long scaled = ((long)authoredFrames * p) / 1000L;
            if (scaled < 1)
            {
                return 1;
            }

            if (scaled > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)scaled;
        }

        private static void ApplyCarryRules(ref MatchState state, in StagePhaseDef phase)
        {
            switch (phase.CarryJewels)
            {
                case StageCarryJewels.None:
                    for (int a = 0; a < state.ActorCount; a++)
                    {
                        JewelRuntime.TryDropAllHeldJewels(ref state, a);
                    }

                    break;
                case StageCarryJewels.DropChance:
                    for (int a = 0; a < state.ActorCount; a++)
                    {
                        ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                        int held = actor.JewelsHeld;
                        for (int n = 0; n < held; n++)
                        {
                            uint roll = (uint)(state.GameplayRng.NextUInt64() % 1000UL);
                            if (roll < (uint)phase.CarryJewelsDropPermille)
                            {
                                JewelRuntime.TryDropOneHeldJewel(ref state, a);
                            }
                        }
                    }

                    break;
            }

            switch (phase.CarryItems)
            {
                case StageCarryItems.None:
                    for (int a = 0; a < state.ActorCount; a++)
                    {
                        ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                        actor.HeldItemSlot = ActorSlot.NoHeldItem;
                        actor.HeldItemSlotLeft = ActorSlot.NoHeldItem;
                    }

                    DespawnFieldItems(ref state);
                    WorldPatchRuntime.InitializeMatch(ref state);
                    break;
                case StageCarryItems.HeldOnly:
                    DespawnFieldItems(ref state);
                    break;
            }
        }

        private static void DespawnFieldItems(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.LooseItems; i++)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, i);
                if (!item.IsActive)
                {
                    continue;
                }

                if (item.HolderSlot != LooseItemSlot.NoHolder)
                {
                    continue;
                }

                item.IsActive = false;
                item.InFlight = false;
                MatchStateAccess.SetLooseItem(ref state, i, item);
            }
        }

        private static void ArmPhaseHazards(ref MatchState state, StageGraph graph)
        {
            ClearHazards(ref state);
            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < phase.Hazards.Length && count < PoolCaps.HazardVolumes; i++)
            {
                StageHazardDef def = phase.Hazards[i];
                var slot = new GenericPoolSlot
                {
                    PosX = def.CenterX,
                    PosY = def.CenterY,
                    PosZ = def.CenterZ,
                    TypeId = def.HazardId,
                    Param0 = 0, // cycle counter
                    Flags = HazardFlagActive,
                    Reserved0 = (byte)def.Trigger,
                    Reserved1 = (ushort)(def.Damage > 65535 ? 65535 : def.Damage),
                };

                if (def.Trigger == StageHazardTrigger.PhaseEnter)
                {
                    // Start telegraph immediately on arm.
                    slot.Param0 = def.PeriodFrames - def.TelegraphFrames;
                    if (slot.Param0 < 0)
                    {
                        slot.Param0 = 0;
                    }

                    slot.Flags |= HazardFlagTelegraph;
                }

                MatchStateAccess.SetHazard(ref state, count, slot);
                // Stash period/telegraph/radius in unused boss-sub slots? Keep period via TypeId lookup in Tick.
                count++;
            }

            state.ActiveHazardCount = count;
        }

        private static void StepHazards(
            ref MatchState state,
            StageGraph graph,
            ref SimContactBuffer events)
        {
            if ((state.StageFlags & StageFlags.HazardsEnabled) == 0)
            {
                return;
            }

            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            for (int i = 0; i < state.ActiveHazardCount; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetHazard(ref state, i);
                if ((slot.Flags & HazardFlagActive) == 0)
                {
                    continue;
                }

                StageHazardDef? def = FindHazard(phase, slot.TypeId);
                if (def == null)
                {
                    continue;
                }

                StageHazardDef hazard = def.Value;
                slot.Param0++;

                int telegraphStart = hazard.PeriodFrames - hazard.TelegraphFrames;
                if (telegraphStart < 0)
                {
                    telegraphStart = 0;
                }

                if (slot.Param0 >= telegraphStart && slot.Param0 < hazard.PeriodFrames)
                {
                    slot.Flags |= HazardFlagTelegraph;
                }
                else
                {
                    slot.Flags = (byte)(slot.Flags & ~HazardFlagTelegraph);
                }

                if (slot.Param0 >= hazard.PeriodFrames)
                {
                    slot.Param0 = 0;
                    slot.Flags = (byte)(slot.Flags & ~HazardFlagTelegraph);
                    if (hazard.Damage > 0)
                    {
                        ApplyHazardPulse(ref state, in hazard, slot.PosX, slot.PosY, slot.PosZ, ref events);
                    }

                    BossAiRuntime.AfterPulse(
                        ref state, hazard.HazardId, slot.PosX, slot.PosZ, hazard.Radius, ref events);

                    if (hazard.Trigger == StageHazardTrigger.PhaseEnter)
                    {
                        // One-shot: disarm.
                        slot.Flags = 0;
                    }
                }

                MatchStateAccess.SetHazard(ref state, i, slot);
            }
        }

        private static StageHazardDef? FindHazard(in StagePhaseDef phase, int hazardId)
        {
            for (int i = 0; i < phase.Hazards.Length; i++)
            {
                if (phase.Hazards[i].HazardId == hazardId)
                {
                    return phase.Hazards[i];
                }
            }

            return null;
        }

        private static void ApplyHazardPulse(
            ref MatchState state,
            in StageHazardDef hazard,
            Fix centerX,
            Fix centerY,
            Fix centerZ,
            ref SimContactBuffer events)
        {
            _ = centerY;
            long radiusSq = MulRaw(hazard.Radius.Raw, hazard.Radius.Raw);
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || actor.InvulnFrames > 0)
                {
                    continue;
                }

                Fix dx = actor.PosX - centerX;
                Fix dz = actor.PosZ - centerZ;
                long distSq = MulRaw(dx.Raw, dx.Raw) + MulRaw(dz.Raw, dz.Raw);
                if (distSq > radiusSq)
                {
                    continue;
                }

                actor.Life -= hazard.Damage;
                if (actor.Life < 0)
                {
                    actor.Life = 0;
                }

                actor.StunFrames = MatchRules.HitStunBase;
                events.Add(SimEventKind.HazardHitActor, (byte)a, (byte)hazard.HazardId);
            }
        }

        private static long MulRaw(long a, long b) => (a * b) >> Fix.FractionBits;

        private static void ActivateKinematics(ref MatchState state, in StagePhaseDef phase)
        {
            int count = 0;
            for (int i = 0; i < phase.Kinematics.Length && count < PoolCaps.KinematicBodies; i++)
            {
                StageKinematicDef def = phase.Kinematics[i];
                var slot = new GenericPoolSlot
                {
                    PosX = def.OriginX,
                    PosY = def.OriginY,
                    PosZ = def.OriginZ,
                    TypeId = def.TrackId,
                    Param0 = def.PeriodFrames,
                    Flags = KinematicFlagActive,
                };
                MatchStateAccess.SetKinematic(ref state, count, slot);
                count++;
            }

            state.ActiveKinematicCount = count;
        }

        private static void StepKinematics(ref MatchState state, StageGraph graph)
        {
            if (!graph.TryGetPhase(state.PhaseIndex, out StagePhaseDef phase))
            {
                return;
            }

            for (int i = 0; i < state.ActiveKinematicCount; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetKinematic(ref state, i);
                if ((slot.Flags & KinematicFlagActive) == 0)
                {
                    continue;
                }

                StageKinematicDef? def = FindKinematic(phase, slot.TypeId);
                if (def == null)
                {
                    continue;
                }

                StageKinematicDef track = def.Value;
                slot.PosX = TriangleOffset(track.OriginX, track.Amplitude, state.Frame, track.PeriodFrames);
                slot.PosZ = track.OriginZ;
                slot.PosY = track.OriginY;
                MatchStateAccess.SetKinematic(ref state, i, slot);
            }
        }

        private static StageKinematicDef? FindKinematic(in StagePhaseDef phase, int trackId)
        {
            for (int i = 0; i < phase.Kinematics.Length; i++)
            {
                if (phase.Kinematics[i].TrackId == trackId)
                {
                    return phase.Kinematics[i];
                }
            }

            return null;
        }

        /// <summary>Deterministic triangle wave offset along X — no float / no trig LUT required.</summary>
        public static Fix TriangleOffset(Fix origin, Fix amplitude, int frame, int periodFrames)
        {
            int period = periodFrames < 1 ? 1 : periodFrames;
            int t = frame % period;
            if (t < 0)
            {
                t += period;
            }

            int half = period / 2;
            if (half < 1)
            {
                return origin;
            }

            int signed = t <= half ? t : (period - t);
            // Map 0..half → -amp..+amp via (signed*2 - half) / half * amp
            long numer = (long)(signed * 2 - half) * amplitude.Raw;
            Fix offset = new Fix(numer / half);
            return origin + offset;
        }

        private static void ClearHazards(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.HazardVolumes; i++)
            {
                MatchStateAccess.SetHazard(ref state, i, default);
            }

            state.ActiveHazardCount = 0;
        }

        private static void ClearKinematics(ref MatchState state)
        {
            for (int i = 0; i < PoolCaps.KinematicBodies; i++)
            {
                MatchStateAccess.SetKinematic(ref state, i, default);
            }

            state.ActiveKinematicCount = 0;
        }
    }
}
