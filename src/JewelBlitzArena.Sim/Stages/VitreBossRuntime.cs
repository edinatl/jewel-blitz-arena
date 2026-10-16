using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>
    /// Chairman Vitre body Life + Overpress KO (stages.md §3.2). Pressworks phase B only.
    /// Flood timeout stays in <see cref="BossTimeoutRuntime"/>.
    /// </summary>
    public static class VitreBossRuntime
    {
        public const byte FlagActive = 1 << 0;
        public const byte FlagCooldown = 1 << 1;
        public const byte FlagShrinking = 1 << 2;

        public const int KindVitreBody = 10;
        public const int KindOverpress = 11;

        public const int IndexBody = 0;
        public const int IndexOverpress = 1;

        public const int VitreMaxLife = 6000;
        public const int OverpressLifePermille = 300; // ≤30%
        public const int OverpressShrinkFrames = 3 * 60;
        public const int OverpressCooldownFrames = 30 * 60;

        public static readonly Fix BodyHitRadius = Fix.FromInt(3);
        public static readonly Fix OverpressStartRadius = Fix.FromInt(10);
        public static readonly Fix OverpressEndRadius = Fix.FromInt(2);

        public const int HazardOverpress = 72;

        public static void InitializeMatch(ref MatchState state, StageGraph graph)
        {
            if (graph.StageId != SampleContentFactory.PressworksStageId)
            {
                return;
            }

            // Lazy-arm body on phase B enter so Sorting hashes stay creature-only.
            ClearPressworksBossSubs(ref state);
        }

        public static void OnPhaseEnter(ref MatchState state, StageGraph graph, int phaseIndex)
        {
            if (graph.StageId != SampleContentFactory.PressworksStageId)
            {
                return;
            }

            if (phaseIndex == 1)
            {
                ArmVitreBody(ref state);
                ArmOverpressController(ref state);
            }
            else
            {
                ClearPressworksBossSubs(ref state);
            }
        }

        public static int BodyLife(ref MatchState state)
        {
            GenericPoolSlot body = MatchStateAccess.GetBossSub(ref state, IndexBody);
            if (body.TypeId != KindVitreBody)
            {
                return 0;
            }

            return body.Param0;
        }

        public static bool IsOverpressArmed(ref MatchState state)
        {
            GenericPoolSlot body = MatchStateAccess.GetBossSub(ref state, IndexBody);
            if (body.TypeId != KindVitreBody || (body.Flags & FlagActive) == 0)
            {
                return false;
            }

            int threshold = (VitreMaxLife * OverpressLifePermille) / 1000;
            return body.Param0 <= threshold;
        }

        public static void ResolveAttackHits(
            ref MatchState state,
            KitRuntime[] kits,
            ref SimContactBuffer events)
        {
            GenericPoolSlot body = MatchStateAccess.GetBossSub(ref state, IndexBody);
            if (body.TypeId != KindVitreBody || (body.Flags & FlagActive) == 0)
            {
                return;
            }

            for (int atk = 0; atk < state.ActorCount; atk++)
            {
                ref ActorSlot attacker = ref MatchStateAccess.Actor(ref state, atk);
                if (!attacker.IsAlive || attacker.IsEliminated)
                {
                    continue;
                }

                if (attacker.ActionPhase != (byte)ActionPhase.Active || attacker.HitApplied != 0)
                {
                    continue;
                }

                if (attacker.Action != (byte)ActorAction.Attack
                    && attacker.Action != (byte)ActorAction.Strong)
                {
                    continue;
                }

                KitRuntime kit = kits[atk];
                long rangeRaw = MatchRules.HitRangeRaw(kit.ReachRating);
                Fix radiusSum = Fix.FromRaw(rangeRaw) + BodyHitRadius;
                if (!PhysicsWorld.OverlapsXZ(
                        attacker.PosX, attacker.PosZ, body.PosX, body.PosZ, radiusSum))
                {
                    continue;
                }

                int damage = attacker.MoveDamage;
                if (damage < 1)
                {
                    damage = 1;
                }

                body.Param0 -= damage;
                if (body.Param0 < 0)
                {
                    body.Param0 = 0;
                }

                attacker.HitApplied = 1;
                events.Add(SimEventKind.BossPartHit, (byte)IndexBody, (byte)atk);
                MatchStateAccess.SetBossSub(ref state, IndexBody, body);
                break;
            }
        }

        public static void Tick(ref MatchState state, ref SimContactBuffer events)
        {
            if (state.StageId != SampleContentFactory.PressworksStageId
                || state.PhaseIndex != 1
                || state.MatchOver)
            {
                return;
            }

            if ((state.StageFlags & StageFlags.InTransition) != 0
                || (state.StageFlags & StageFlags.HazardsEnabled) == 0)
            {
                return;
            }

            GenericPoolSlot over = MatchStateAccess.GetBossSub(ref state, IndexOverpress);
            if (over.TypeId != KindOverpress)
            {
                return;
            }

            if ((over.Flags & FlagShrinking) != 0)
            {
                TickShrink(ref state, ref over, ref events);
                return;
            }

            if ((over.Flags & FlagCooldown) != 0)
            {
                if (over.Param0 > 0)
                {
                    over.Param0--;
                }

                if (over.Param0 <= 0)
                {
                    over.Flags = 0;
                    over.Param0 = 0;
                }

                MatchStateAccess.SetBossSub(ref state, IndexOverpress, over);
                return;
            }

            // Idle — start Overpress when Life ≤ 30%.
            if (!IsOverpressArmed(ref state))
            {
                return;
            }

            over.Flags = FlagShrinking;
            over.Param0 = OverpressShrinkFrames;
            MatchStateAccess.SetBossSub(ref state, IndexOverpress, over);
            events.Add(SimEventKind.HazardHitActor, 255, (byte)HazardOverpress);
        }

        /// <summary>Current Overpress ring radius while shrinking (0 if not shrinking).</summary>
        public static bool TryGetShrinkRadius(ref MatchState state, out Fix radius)
        {
            radius = Fix.Zero;
            GenericPoolSlot over = MatchStateAccess.GetBossSub(ref state, IndexOverpress);
            if (over.TypeId != KindOverpress || (over.Flags & FlagShrinking) == 0)
            {
                return false;
            }

            radius = RadiusAt(over.Param0);
            return true;
        }

        private static void TickShrink(
            ref MatchState state,
            ref GenericPoolSlot over,
            ref SimContactBuffer events)
        {
            if (over.Param0 > 0)
            {
                over.Param0--;
            }

            if (over.Param0 > 0)
            {
                MatchStateAccess.SetBossSub(ref state, IndexOverpress, over);
                return;
            }

            // Collapse: KO anyone still inside the end radius.
            Fix radius = OverpressEndRadius;
            for (int a = 0; a < state.ActorCount; a++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, a);
                if (!actor.IsAlive || actor.IsEliminated || actor.InvulnFrames > 0)
                {
                    continue;
                }

                if (!PhysicsWorld.OverlapsXZ(
                        actor.PosX, actor.PosZ, Fix.Zero, Fix.Zero, radius))
                {
                    continue;
                }

                events.Add(SimEventKind.HazardHitActor, (byte)a, (byte)HazardOverpress);
                CombatSystem.ForceKnockOut(ref state, a);
            }

            over.Flags = FlagCooldown;
            over.Param0 = OverpressCooldownFrames;
            MatchStateAccess.SetBossSub(ref state, IndexOverpress, over);
        }

        private static Fix RadiusAt(int framesRemaining)
        {
            // framesRemaining goes OverpressShrinkFrames → 0.
            int elapsed = OverpressShrinkFrames - framesRemaining;
            if (elapsed < 0)
            {
                elapsed = 0;
            }

            if (elapsed > OverpressShrinkFrames)
            {
                elapsed = OverpressShrinkFrames;
            }

            long start = OverpressStartRadius.Raw;
            long end = OverpressEndRadius.Raw;
            long raw = start + ((end - start) * elapsed) / OverpressShrinkFrames;
            return new Fix(raw);
        }

        private static void ArmVitreBody(ref MatchState state)
        {
            var body = new GenericPoolSlot
            {
                PosX = Fix.Zero,
                PosY = Fix.Zero,
                PosZ = Fix.Zero,
                TypeId = KindVitreBody,
                Param0 = VitreMaxLife,
                Flags = FlagActive,
                Reserved1 = (ushort)(VitreMaxLife > 65535 ? 65535 : VitreMaxLife),
            };
            MatchStateAccess.SetBossSub(ref state, IndexBody, body);
        }

        private static void ArmOverpressController(ref MatchState state)
        {
            var over = new GenericPoolSlot
            {
                TypeId = KindOverpress,
                Param0 = 0,
                Flags = 0,
            };
            MatchStateAccess.SetBossSub(ref state, IndexOverpress, over);
        }

        private static void ClearPressworksBossSubs(ref MatchState state)
        {
            MatchStateAccess.SetBossSub(ref state, IndexBody, default);
            MatchStateAccess.SetBossSub(ref state, IndexOverpress, default);
        }
    }
}
