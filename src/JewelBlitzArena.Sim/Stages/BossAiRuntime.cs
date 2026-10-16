namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Boss-arena targeting beyond stub timers (stages.md §3.1 / §3.2).
    /// Harvester: footfall follows patrol kin; intake tracks nearest fighter; stomp applies Dizzy.
    /// Pressworks: Live Hands chase nearest fighter. Ban-safe Fix / slot-index only.
    /// </summary>
    public static class BossAiRuntime
    {
        public const int FootfallShadowId = 50;
        public const int IntakeConeId = 51;
        public const int CuttingBeamLineId = 52;
        public const int StompShockId = 53;
        public const int LiveHandsChaseId = 63;

        public const int HarvesterPatrolTrackId = 50;

        /// <summary>~0.08 m/frame chase step (Fix 1/12).</summary>
        public static readonly Fix ChaseStep = Fix.One / 12;

        /// <summary>Intake throw impulse toward maw (origin) on pulse.</summary>
        public static readonly Fix IntakeThrowSpeed = Fix.FromInt(3);

        /// <summary>
        /// Bind moving hazard centers after kinematics step, before hazard pulses.
        /// No-op for stages without boss hazard ids.
        /// </summary>
        public static void BindCenters(ref MatchState state, StageGraph graph)
        {
            if ((state.StageFlags & StageFlags.HazardsEnabled) == 0
                || state.ActiveHazardCount <= 0)
            {
                return;
            }

            if (!graph.TryGetPhase(state.PhaseIndex, out _))
            {
                return;
            }

            bool hasPatrol = TryGetKinematicPos(
                ref state, HarvesterPatrolTrackId, out Fix patrolX, out Fix patrolZ);

            for (int i = 0; i < state.ActiveHazardCount; i++)
            {
                GenericPoolSlot slot = MatchStateAccess.GetHazard(ref state, i);
                if ((slot.Flags & StageRuntime.HazardFlagActive) == 0)
                {
                    continue;
                }

                switch (slot.TypeId)
                {
                    case FootfallShadowId:
                        if (hasPatrol)
                        {
                            slot.PosX = patrolX;
                            slot.PosZ = patrolZ;
                            MatchStateAccess.SetHazard(ref state, i, slot);
                        }

                        break;

                    case IntakeConeId:
                    case LiveHandsChaseId:
                    {
                        int nearest = FindNearestLivingActor(ref state, slot.PosX, slot.PosZ);
                        if (nearest >= 0)
                        {
                            ref ActorSlot target = ref MatchStateAccess.Actor(ref state, nearest);
                            StepToward(ref slot.PosX, ref slot.PosZ, target.PosX, target.PosZ, ChaseStep);
                            MatchStateAccess.SetHazard(ref state, i, slot);
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Extra pulse effects when Damage is 0 or when a boss hazard needs knockback (Intake).
        /// Called after the standard Life pulse (if any).
        /// </summary>
        public static void AfterPulse(
            ref MatchState state,
            int hazardId,
            Fix centerX,
            Fix centerZ,
            Fix radius,
            ref SimContactBuffer events)
        {
            if (hazardId == StompShockId)
            {
                ApplyStompDizzy(ref state, centerX, centerZ, radius, ref events);
                return;
            }

            if (hazardId == IntakeConeId)
            {
                ApplyIntakeThrow(ref state, centerX, centerZ, radius);
            }
        }

        public static void ApplyStompDizzy(
            ref MatchState state,
            Fix centerX,
            Fix centerZ,
            Fix radius,
            ref SimContactBuffer events)
        {
            long radiusSq = MulRaw(radius.Raw, radius.Raw);
            int frames = MatchRules.DizzyDurationFrames;
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || actor.InvulnFrames > 0)
                {
                    continue;
                }

                // Grounded-only: airborne fighters (PosY > 0) skip Stomp Dizzy.
                if (actor.PosY.Raw > 0)
                {
                    continue;
                }

                if (actor.DizzyImmuneFrames > 0)
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

                if (frames > actor.DizzyFrames)
                {
                    actor.DizzyFrames = (ushort)frames;
                }

                events.Add(SimEventKind.HazardHitActor, (byte)a, (byte)StompShockId);
            }
        }

        private static void ApplyIntakeThrow(
            ref MatchState state,
            Fix centerX,
            Fix centerZ,
            Fix radius)
        {
            long radiusSq = MulRaw(radius.Raw, radius.Raw);
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

                // Throw toward maw (origin): velocity from actor toward (0,0) on XZ.
                Fix toMawX = -actor.PosX;
                Fix toMawZ = -actor.PosZ;
                long magSq = MulRaw(toMawX.Raw, toMawX.Raw) + MulRaw(toMawZ.Raw, toMawZ.Raw);
                if (magSq <= 0)
                {
                    actor.VelX = -IntakeThrowSpeed;
                    actor.VelZ = Fix.Zero;
                    continue;
                }

                // Approximate unit direction via dominant axis (Ban-safe; no sqrt).
                if (AbsRaw(toMawX.Raw) >= AbsRaw(toMawZ.Raw))
                {
                    actor.VelX = toMawX.Raw >= 0 ? IntakeThrowSpeed : -IntakeThrowSpeed;
                    actor.VelZ = Fix.Zero;
                }
                else
                {
                    actor.VelX = Fix.Zero;
                    actor.VelZ = toMawZ.Raw >= 0 ? IntakeThrowSpeed : -IntakeThrowSpeed;
                }
            }
        }

        /// <summary>Nearest living actor to (originX, originZ); lowest slot wins ties. −1 if none.</summary>
        public static int FindNearestLivingActor(ref MatchState state, Fix originX, Fix originZ)
        {
            int best = -1;
            long bestDistSq = long.MaxValue;
            for (int i = 0; i < state.ActorCount; i++)
            {
                ActorSlot other = MatchStateAccess.Actor(ref state, i);
                if (!other.IsAlive || other.IsEliminated)
                {
                    continue;
                }

                long dx = other.PosX.Raw - originX.Raw;
                long dz = other.PosZ.Raw - originZ.Raw;
                long distSq = (dx * dx) + (dz * dz);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = i;
                }
            }

            return best;
        }

        private static bool TryGetKinematicPos(
            ref MatchState state, int trackId, out Fix posX, out Fix posZ)
        {
            for (int i = 0; i < state.ActiveKinematicCount; i++)
            {
                GenericPoolSlot kin = MatchStateAccess.GetKinematic(ref state, i);
                if ((kin.Flags & StageRuntime.KinematicFlagActive) == 0)
                {
                    continue;
                }

                if (kin.TypeId == trackId)
                {
                    posX = kin.PosX;
                    posZ = kin.PosZ;
                    return true;
                }
            }

            posX = Fix.Zero;
            posZ = Fix.Zero;
            return false;
        }

        private static void StepToward(
            ref Fix posX, ref Fix posZ, Fix targetX, Fix targetZ, Fix step)
        {
            Fix dx = targetX - posX;
            Fix dz = targetZ - posZ;
            long adx = AbsRaw(dx.Raw);
            long adz = AbsRaw(dz.Raw);
            if (adx == 0 && adz == 0)
            {
                return;
            }

            // Move on dominant axis first so chase is hash-stable without sqrt.
            if (adx >= adz)
            {
                if (adx <= step.Raw)
                {
                    posX = targetX;
                }
                else
                {
                    posX = dx.Raw >= 0 ? posX + step : posX - step;
                }
            }
            else
            {
                if (adz <= step.Raw)
                {
                    posZ = targetZ;
                }
                else
                {
                    posZ = dz.Raw >= 0 ? posZ + step : posZ - step;
                }
            }
        }

        private static long MulRaw(long a, long b) => (a * b) >> Fix.FractionBits;

        private static long AbsRaw(long v) => v < 0 ? -v : v;
    }
}
