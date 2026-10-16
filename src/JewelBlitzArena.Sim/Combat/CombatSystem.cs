using JewelBlitzArena.Data;

namespace JewelBlitzArena.Sim
{
    /// <summary>Attack strings, hits, knockdown, dodge arm, Blitz Luster — S1 match rules.</summary>
    public static class CombatSystem
    {
        public static void ApplyInput(
            ref MatchState state,
            int index,
            in InputFrame input,
            KitRuntime kit,
            Fix dtWalk,
            Fix dtRun,
            Fix dtFull)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, index);
            if (!actor.IsAlive || actor.IsEliminated)
            {
                actor.VelX = Fix.Zero;
                actor.VelY = Fix.Zero;
                actor.VelZ = Fix.Zero;
                return;
            }

            if (actor.BodyPropFrames > 0 || actor.TransformFrames > 0 || actor.RevertFrames > 0)
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = Fix.Zero;
                return;
            }

            if (actor.KnockdownFrames > 0)
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = Fix.Zero;
                if (actor.KnockdownFrames <= MatchRules.KnockdownMaxFrames - MatchRules.KnockdownMinFrames
                    && (input.Attack || input.Jump || input.Action || input.Strong))
                {
                    actor.KnockdownFrames = 0;
                    actor.InvulnFrames = 20;
                    actor.Action = (byte)ActorAction.Getup;
                    actor.ActionTimer = 20;
                    actor.ActionPhase = (byte)ActionPhase.Recovery;
                }

                return;
            }

            if (actor.StunFrames > 0 || actor.DizzyFrames > 0 || actor.FreezeFrames > 0)
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = Fix.Zero;
                // Dizzy mash: shave one extra frame while buttons held, down to ~70% floor.
                if (actor.DizzyFrames > 0
                    && (input.Attack || input.Jump || input.Action || input.Strong)
                    && actor.DizzyFrames > MatchRules.DizzyMashFloorFrames)
                {
                    actor.DizzyFrames--;
                }

                return;
            }

            bool canMove = actor.ActionPhase != (byte)ActionPhase.Startup
                && actor.ActionPhase != (byte)ActionPhase.Active
                && actor.PinFrames == 0;
            if (canMove)
            {
                Fix step = MagnitudeToStep(input.Magnitude, dtWalk, dtRun, dtFull);
                if (actor.SlowFrames > 0 && step.Raw != 0)
                {
                    // −40% move speed (balance.md §4).
                    step = new Fix((step.Raw * 3) / 5);
                }

                if (step.Raw == 0)
                {
                    actor.VelX = Fix.Zero;
                    actor.VelZ = Fix.Zero;
                }
                else
                {
                    FixVec2 dir = DirectionLut.GetUnit(input.Direction);
                    actor.VelX = dir.X * step;
                    actor.VelZ = dir.Y * step;
                    // Blinded: auto-facing off (balance.md §4).
                    if (actor.BlindFrames == 0)
                    {
                        actor.Facing = dir.X.Raw < 0 ? (byte)1 : (byte)0;
                    }
                }
            }
            else
            {
                actor.VelX = Fix.Zero;
                actor.VelZ = Fix.Zero;
            }

            if (input.Jump && input.Magnitude > 0 && actor.DodgeCooldown <= 0
                && actor.Action == (byte)ActorAction.Neutral)
            {
                actor.DodgeArmed = MatchRules.DodgeArmFrames;
            }

            if (actor.Action != (byte)ActorAction.Neutral && actor.ActionTimer > 0)
            {
                return;
            }

            // Item throws: Attack → right hand; Strong → left hand (Clank). Single-hold kits
            // still block Strong while holding the right-hand item.
            if (input.Attack && actor.HeldItemSlot != ActorSlot.NoHeldItem)
            {
                return;
            }

            if (input.Strong && actor.HeldItemSlotLeft != ActorSlot.NoHeldItem)
            {
                return;
            }

            if (input.Strong
                && actor.HeldItemSlot != ActorSlot.NoHeldItem
                && actor.HeldItemSlotLeft == ActorSlot.NoHeldItem
                && !actor.IsDualHold)
            {
                return;
            }

            if (actor.IsBlitzed)
            {
                var behavior = (KitBehavior)actor.KitBehavior;
                if (input.SurgeA)
                {
                    TryStartMove(
                        ref state,
                        index,
                        kit.ResolveSurgeA(in input, actor.Facing),
                        ActorAction.SurgeA,
                        MatchRules.SurgeLusterCostFor(behavior, surgeA: true));
                    return;
                }

                if (input.SurgeB)
                {
                    TryStartMove(
                        ref state,
                        index,
                        kit.ResolveSurgeB(in input, actor.Facing),
                        ActorAction.SurgeB,
                        MatchRules.SurgeLusterCostFor(behavior, surgeA: false));
                    return;
                }

                if (input.Attack)
                {
                    TryStartMove(ref state, index, kit.Flare, ActorAction.Flare, lusterCost: 0);
                    return;
                }

                if (input.Strong)
                {
                    TryStartMove(ref state, index, kit.Strong, ActorAction.Strong, lusterCost: 0);
                }

                return;
            }

            if (input.Attack && input.Strong)
            {
                TryStartMove(ref state, index, kit.Throw, ActorAction.Throw, lusterCost: 0);
                return;
            }

            if (input.Strong)
            {
                TryStartMove(ref state, index, kit.Strong, ActorAction.Strong, lusterCost: 0);
                return;
            }

            if (input.Attack)
            {
                TryStartMove(ref state, index, kit.Attack, ActorAction.Attack, lusterCost: 0);
            }
        }

        public static void TickActors(ref MatchState state, KitRuntime[] kits)
        {
            for (int i = 0; i < state.ActorCount; i++)
            {
                ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, i);
                if (!actor.IsAlive && actor.BodyPropFrames <= 0)
                {
                    continue;
                }

                TickTimers(ref state, i);
                TickAction(ref actor, kits[i]);
            }

            ResolveHits(ref state, kits);
        }

        /// <summary>S1 consumes S2 contact events: Blitz enter + thrown-item damage.</summary>
        public static void ConsumeContacts(ref MatchState state, ref SimContactBuffer contacts)
        {
            for (int i = 0; i < contacts.Count; i++)
            {
                SimContactEvent ev = contacts.Get(i);
                switch (ev.Kind)
                {
                    case SimEventKind.JewelReadyToBlitz:
                        {
                            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, ev.SlotA);
                            if (actor.IsAlive && !actor.IsEliminated && !actor.IsBlitzed
                                && actor.JewelsHeld >= MatchRules.BlitzJewelCount
                                && !StageRuntime.IsBlitzSuppressed(in state))
                            {
                                JewelSystem.BeginBlitz(ref state, ev.SlotA);
                            }

                            break;
                        }

                    case SimEventKind.ThrownItemHitActor:
                        {
                            // SlotA = loose item index, SlotB = victim.
                            int itemSlot = ev.SlotA;
                            int victim = ev.SlotB;
                            if (victim < 0 || victim >= state.ActorCount)
                            {
                                break;
                            }

                            ApplyThrownItemHit(ref state, itemSlot, victim);
                            break;
                        }

                    case SimEventKind.ItemLanded:
                        {
                            int itemSlot = ev.SlotA;
                            if (itemSlot < 0 || itemSlot >= PoolCaps.LooseItems)
                            {
                                break;
                            }

                            LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, itemSlot);
                            WorldPatchRuntime.TrySpawnFromItem(
                                ref state, item.ItemId, item.PosX, item.PosZ);
                            break;
                        }
                }
            }
        }

        private static void TickTimers(ref MatchState state, int actorIndex)
        {
            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (actor.DodgeArmed > 0) actor.DodgeArmed--;
            if (actor.DodgeCooldown > 0) actor.DodgeCooldown--;
            if (actor.DrossFrames > 0) actor.DrossFrames--;

            if (actor.StunFrames > 0)
            {
                actor.StunFrames--;
                if (actor.StunFrames == 0 && actor.ComboResetTimer <= 0)
                {
                    actor.ComboResetTimer = MatchRules.ComboResetFrames;
                }
            }

            if (actor.BlindFrames > 0)
            {
                actor.BlindFrames--;
            }

            if (actor.DizzyFrames > 0)
            {
                actor.DizzyFrames--;
                if (actor.DizzyFrames == 0)
                {
                    actor.DizzyImmuneFrames = MatchRules.DizzyImmuneFrames;
                }
            }
            else if (actor.DizzyImmuneFrames > 0)
            {
                actor.DizzyImmuneFrames--;
            }

            if (actor.SlowFrames > 0)
            {
                actor.SlowFrames--;
            }

            if (actor.PinFrames > 0)
            {
                actor.PinFrames--;
                if (actor.PinFrames == 0)
                {
                    actor.PinImmuneFrames = MatchRules.PinImmuneFrames;
                }
            }
            else if (actor.PinImmuneFrames > 0)
            {
                actor.PinImmuneFrames--;
            }

            if (actor.FreezeFrames > 0)
            {
                actor.FreezeFrames--;
                if (actor.FreezeFrames == 0)
                {
                    actor.FreezeImmuneFrames = MatchRules.FreezeImmuneFrames;
                }
            }
            else if (actor.FreezeImmuneFrames > 0)
            {
                actor.FreezeImmuneFrames--;
            }

            if (actor.ComboResetTimer > 0)
            {
                actor.ComboResetTimer--;
                if (actor.ComboResetTimer == 0)
                {
                    actor.ComboChain = 0;
                    actor.ComboDamage = 0;
                }
            }

            if (actor.KnockdownFrames > 0)
            {
                actor.KnockdownFrames--;
                if (actor.KnockdownFrames == 0)
                {
                    actor.InvulnFrames = 20;
                }
            }

            if (actor.InvulnFrames > 0) actor.InvulnFrames--;

            if (actor.TransformFrames > 0)
            {
                actor.TransformFrames--;
                if (actor.TransformFrames == 0)
                {
                    ClearAction(ref actor);
                }
            }

            if (actor.RevertFrames > 0)
            {
                actor.RevertFrames--;
                if (actor.RevertFrames == 0)
                {
                    ClearAction(ref actor);
                }
            }

            if (actor.BodyPropFrames > 0) actor.BodyPropFrames--;

            if (actor.AttackStringTimer > 0)
            {
                actor.AttackStringTimer--;
                if (actor.AttackStringTimer == 0)
                {
                    actor.AttackStringStep = 0;
                }
            }

            if (actor.IsBlitzed
                && actor.TransformFrames <= 0
                && (state.HouseRulesBits & MatchRules.HouseLusterTimer) != 0)
            {
                actor.Luster -= MatchRules.LusterDrainPerFrame;
                if (actor.Luster <= 0)
                {
                    JewelSystem.OnRevert(ref state, actorIndex);
                }
            }
        }

        private static void TickAction(ref ActorSlot actor, KitRuntime kit)
        {
            if (actor.ActionTimer <= 0 || actor.Action == (byte)ActorAction.Neutral)
            {
                if (actor.ActionTimer <= 0)
                {
                    ClearAction(ref actor);
                }

                return;
            }

            actor.ActionTimer--;
            MoveRecord? move = MoveForAction(kit, (ActorAction)actor.Action);
            if (move == null)
            {
                ClearAction(ref actor);
                return;
            }

            int startup = move.StartupFrames;
            int active = move.ActiveFrames;
            int recovery = move.RecoveryFrames;
            int elapsed = startup + active + recovery - actor.ActionTimer;

            if (elapsed < startup)
            {
                actor.ActionPhase = (byte)ActionPhase.Startup;
            }
            else if (elapsed < startup + active)
            {
                actor.ActionPhase = (byte)ActionPhase.Active;
            }
            else
            {
                actor.ActionPhase = (byte)ActionPhase.Recovery;
            }

            if (actor.ActionTimer == 0)
            {
                ClearAction(ref actor);
            }
        }

        private static void ResolveHits(ref MatchState state, KitRuntime[] kits)
        {
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

                KitRuntime kit = kits[atk];
                long rangeRaw = MatchRules.HitRangeRaw(kit.ReachRating);
                long rangeSq = rangeRaw * rangeRaw;

                for (int vic = 0; vic < state.ActorCount; vic++)
                {
                    if (vic == atk)
                    {
                        continue;
                    }

                    ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, vic);
                    if (!victim.IsAlive || victim.IsEliminated || victim.InvulnFrames > 0)
                    {
                        continue;
                    }

                    if (DistSq(attacker.PosX, attacker.PosZ, victim.PosX, victim.PosZ) > rangeSq)
                    {
                        continue;
                    }

                    if (victim.DodgeArmed > 0 && attacker.Action != (byte)ActorAction.Throw)
                    {
                        victim.DodgeArmed = 0;
                        victim.InvulnFrames = MatchRules.DodgeInvulnFrames;
                        victim.DodgeCooldown = MatchRules.DodgeCooldownFrames;
                        victim.Action = (byte)ActorAction.Dodge;
                        victim.ActionTimer = MatchRules.DodgeInvulnFrames;
                        victim.ActionPhase = (byte)ActionPhase.Active;
                        attacker.HitApplied = 1;
                        continue;
                    }

                    int damage = attacker.MoveDamage;
                    if (victim.IsBlitzed)
                    {
                        damage = (damage * MatchRules.BlitzedDamageTakenPercent) / 100;
                    }

                    ApplyHit(ref state, atk, vic, damage);
                    attacker.HitApplied = 1;
                }
            }
        }

        private static void ApplyThrownItemHit(ref MatchState state, int itemSlot, int vic)
        {
            ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, vic);
            if (!victim.IsAlive || victim.IsEliminated || victim.InvulnFrames > 0)
            {
                return;
            }

            int itemId = 0;
            Fix impactX = victim.PosX;
            Fix impactZ = victim.PosZ;
            if (itemSlot >= 0 && itemSlot < PoolCaps.LooseItems)
            {
                LooseItemSlot item = MatchStateAccess.GetLooseItem(ref state, itemSlot);
                itemId = item.ItemId;
                impactX = item.PosX;
                impactZ = item.PosZ;
            }

            if (itemId == MatchRules.PetrifyGasItemId)
            {
                StatusRuntime.ApplyFreeze(ref state, vic, MatchRules.FreezeDurationFrames);
            }
            else if (itemId == MatchRules.IceJarItemId
                || itemId == MatchRules.SmokeJarItemId
                || itemId == MatchRules.AdhesiveSprayItemId)
            {
                // Lasting floor patch at impact; first pulse applies this frame via Tick.
                WorldPatchRuntime.TrySpawnFromItem(ref state, itemId, impactX, impactZ);
            }

            const int thrownDamage = 40;
            int damage = ScaleIncomingDamage(ref victim, thrownDamage);
            if (victim.IsBlitzed)
            {
                damage = (damage * MatchRules.BlitzedDamageTakenPercent) / 100;
            }

            victim.Life -= damage;
            victim.LastHitFrame = state.Frame;
            victim.StunFrames = MatchRules.HitStunBase;
            if (victim.Life <= 0)
            {
                KnockOut(ref state, atk: -1, vic);
            }
        }

        private static void ApplyHit(ref MatchState state, int atk, int vic, int damage)
        {
            ref ActorSlot attacker = ref MatchStateAccess.Actor(ref state, atk);
            ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, vic);

            if (damage < 0)
            {
                damage = 0;
            }

            damage = ScaleIncomingDamage(ref victim, damage);

            victim.Life -= damage;
            victim.LastHitFrame = state.Frame;
            victim.ComboChain++;
            victim.ComboDamage += damage;
            victim.ComboResetTimer = MatchRules.ComboResetFrames;
            victim.StunFrames = MatchRules.HitStunBase;

            bool forceKd = victim.ComboChain >= MatchRules.ComboForceKnockdownAt
                || attacker.Action == (byte)ActorAction.Strong
                || attacker.Action == (byte)ActorAction.Throw;

            if (forceKd)
            {
                victim.KnockdownFrames = MatchRules.KnockdownMaxFrames;
                victim.StunFrames = 0;
                victim.Action = (byte)ActorAction.Knockdown;
                victim.ActionTimer = MatchRules.KnockdownMaxFrames;
                victim.ActionPhase = (byte)ActionPhase.Recovery;
                JewelSystem.OnKnockdownJewelDrop(ref state, vic);
            }

            // Light hits nudge; Strong/Throw use a larger shove. S2 replaces with physics impulses.
            Fix kb = forceKd ? (Fix.FromInt(1) / 2) : (Fix.FromInt(1) / 8);
            victim.PosX += attacker.Facing == 0 ? kb : -kb;

            if (victim.Life <= 0)
            {
                KnockOut(ref state, atk, vic);
            }
        }

        private static void KnockOut(ref MatchState state, int atk, int vic)
        {
            ref ActorSlot victim = ref MatchStateAccess.Actor(ref state, vic);

            victim.Life = 0;
            victim.IsAlive = false;
            victim.IsEliminated = true;
            victim.IsBlitzed = false;
            victim.Luster = 0;
            victim.JewelsHeld = 0;
            victim.BodyPropFrames = MatchRules.BodyPropFrames;
            ClearAction(ref victim);
            victim.VelX = Fix.Zero;
            victim.VelZ = Fix.Zero;
            if (atk >= 0 && atk < state.ActorCount && atk != vic)
            {
                MatchStateAccess.Actor(ref state, atk).KosDealt++;
            }

            MatchStateAccess.SetPlacement(ref state, vic, state.NextPlacementFromBottom);

            state.NextPlacementFromBottom--;

            int living = 0;
            int lastLiving = -1;
            for (int i = 0; i < state.ActorCount; i++)
            {
                ref ActorSlot a = ref MatchStateAccess.Actor(ref state, i);
                if (a.IsAlive && !a.IsEliminated)
                {
                    living++;
                    lastLiving = i;
                }
            }

            if (living <= 1 && state.MatchEndFrame == 0)
            {
                if (lastLiving >= 0)
                {
                    MatchStateAccess.SetPlacement(ref state, lastLiving, 1);
                }

                state.MatchEndFrame = state.Frame + 1;
            }
        }

        private static void ClearAction(ref ActorSlot actor)
        {
            actor.Action = (byte)ActorAction.Neutral;
            actor.ActionPhase = (byte)ActionPhase.None;
            actor.ActionTimer = 0;
            actor.HitApplied = 0;
        }

        private static void TryStartMove(
            ref MatchState state,
            int actorIndex,
            MoveRecord? move,
            ActorAction action,
            int lusterCost)
        {
            if (move == null)
            {
                return;
            }

            ref ActorSlot actor = ref MatchStateAccess.Actor(ref state, actorIndex);
            if (lusterCost > 0)
            {
                if (actor.Luster < lusterCost)
                {
                    return;
                }

                actor.Luster -= lusterCost;
            }

            int total = move.StartupFrames + move.ActiveFrames + move.RecoveryFrames;
            if (total <= 0)
            {
                total = 1;
            }

            actor.Action = (byte)action;
            actor.ActionPhase = (byte)ActionPhase.Startup;
            actor.ActionTimer = total;
            actor.HitApplied = 0;
            actor.AttackStringTimer = MatchRules.AttackStringWindowFrames;
            if (action == ActorAction.Attack)
            {
                actor.AttackStringStep++;
            }

            switch (move.Effect)
            {
                case MoveEffect.Heal:
                    ApplySelfHeal(ref actor, move.Damage);
                    actor.MoveDamage = 0;
                    break;
                case MoveEffect.Magnet:
                    actor.MoveDamage = 0;
                    ItemRuntime.ApplyMagnetSurge(ref state, actorIndex, MatchRules.MagnetSurgeRange);
                    break;
                case MoveEffect.Utility:
                    actor.MoveDamage = 0;
                    break;
                case MoveEffect.Stun:
                    actor.MoveDamage = move.Damage;
                    StatusRuntime.ApplyAreaStun(
                        ref state,
                        actorIndex,
                        MatchRules.BateriaStunRange,
                        MatchRules.BateriaStunFrames,
                        groundedOnly: true);
                    break;
                case MoveEffect.Blind:
                    actor.MoveDamage = move.Damage;
                    StatusRuntime.ApplyAreaBlind(
                        ref state,
                        actorIndex,
                        MatchRules.ChaffBlindRange,
                        MatchRules.BlindDurationFrames);
                    break;
                case MoveEffect.Dizzy:
                    actor.MoveDamage = 0;
                    StatusRuntime.ApplyAreaDizzy(
                        ref state,
                        actorIndex,
                        MatchRules.SmokeVaultDizzyRange,
                        MatchRules.DizzyDurationFrames);
                    break;
                case MoveEffect.Slow:
                    actor.MoveDamage = move.Damage;
                    StatusRuntime.ApplyAreaSlow(
                        ref state,
                        actorIndex,
                        MatchRules.MineFieldSlowRange,
                        MatchRules.SlowDurationFrames);
                    break;
                case MoveEffect.Pin:
                    actor.MoveDamage = move.Damage;
                    StatusRuntime.ApplyAreaPin(
                        ref state,
                        actorIndex,
                        MatchRules.HoldfastPinRange,
                        MatchRules.PinDurationFrames);
                    break;
                case MoveEffect.Freeze:
                    actor.MoveDamage = move.Damage;
                    StatusRuntime.ApplyAreaFreeze(
                        ref state,
                        actorIndex,
                        MatchRules.FreezeAreaRange,
                        MatchRules.FreezeDurationFrames);
                    break;
                default:
                    actor.MoveDamage = move.Damage;
                    break;
            }
        }

        /// <summary>
        /// Freeze / Petrify: 50% damage taken; raw hits ≥ 100 break early (balance.md §4).
        /// </summary>
        private static int ScaleIncomingDamage(ref ActorSlot victim, int damage)
        {
            if (damage < 0)
            {
                return 0;
            }

            if (victim.FreezeFrames == 0)
            {
                return damage;
            }

            if (damage >= MatchRules.FreezeBreakDamage)
            {
                victim.FreezeFrames = 0;
                victim.FreezeImmuneFrames = MatchRules.FreezeImmuneFrames;
            }

            return (damage * MatchRules.FreezeDamageTakenPercent) / 100;
        }

        private static void ApplySelfHeal(ref ActorSlot actor, int amount)
        {
            if (amount <= 0 || !actor.IsAlive || actor.IsEliminated)
            {
                return;
            }

            int next = actor.Life + amount;
            if (next > actor.MaxLife)
            {
                next = actor.MaxLife;
            }

            actor.Life = next;
        }

        private static MoveRecord? MoveForAction(KitRuntime kit, ActorAction action)
        {
            switch (action)
            {
                case ActorAction.Attack: return kit.Attack;
                case ActorAction.Strong: return kit.Strong;
                case ActorAction.Flare: return kit.Flare;
                case ActorAction.SurgeA: return kit.SurgeA;
                case ActorAction.SurgeB: return kit.SurgeB;
                case ActorAction.Throw: return kit.Throw;
                default: return null;
            }
        }

        private static Fix MagnitudeToStep(int magnitude, Fix dtWalk, Fix dtRun, Fix dtFull)
        {
            switch (magnitude)
            {
                case 1: return dtWalk;
                case 2: return dtRun;
                case 3: return dtFull;
                default: return Fix.Zero;
            }
        }

        private static long DistSq(Fix ax, Fix az, Fix bx, Fix bz)
        {
            long dx = ax.Raw - bx.Raw;
            long dz = az.Raw - bz.Raw;
            return dx * dx + dz * dz;
        }
    }
}
