using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    /// <summary>Authored pack → runtime catalog (decimal strings → Q48.16 raw).</summary>
    public static class ContentCompiler
    {
        public const int TickRateHz = 60;

        public static ContentCatalog Compile(AuthoredContentPack pack)
        {
            ValidationResult validation = ContentValidator.Validate(pack);
            if (!validation.IsOk)
            {
                throw new InvalidOperationException(
                    "Content failed validation: " + string.Join("; ", validation.Errors));
            }

            var fighters = new List<FighterKitRecord>(pack.Fighters.Count);
            foreach (AuthoredFighter src in pack.Fighters)
            {
                int maxLife = BalanceBudgets.MaxLifeForRating(src.LifeRating);
                var moves = new List<MoveRecord>(src.Moves.Count);
                foreach (AuthoredMove m in src.Moves)
                {
                    ContentValidator.TryParseMoveClass(m.Class, out MoveClass moveClass);
                    moves.Add(new MoveRecord
                    {
                        Name = m.Name,
                        Class = moveClass,
                        Damage = ApplyPowerScale(m.Damage, moveClass, src.PowerRating, m.UtilityExempt),
                        StartupFrames = m.StartupFrames,
                        ActiveFrames = m.ActiveFrames,
                        RecoveryFrames = m.RecoveryFrames,
                        UtilityExempt = m.UtilityExempt || moveClass == MoveClass.UtilityStrong,
                    });
                }

                ContentValidator.TryParseKitBehavior(src.KitBehavior, out byte kitBehavior);
                fighters.Add(new FighterKitRecord
                {
                    FighterId = src.FighterId,
                    Name = src.Name,
                    SpeedRating = src.SpeedRating,
                    LifeRating = src.LifeRating,
                    MaxLife = maxLife,
                    ReachRating = src.ReachRating,
                    AirRating = src.AirRating,
                    PowerRating = src.PowerRating,
                    WalkSpeedRaw = DecimalFixParser.ParseToRaw(src.WalkSpeed),
                    RunSpeedRaw = DecimalFixParser.ParseToRaw(src.RunSpeed),
                    FullSpeedRaw = DecimalFixParser.ParseToRaw(src.FullSpeed),
                    KitBehavior = kitBehavior,
                    Moves = moves.ToArray(),
                });
            }

            var stages = new List<StageRecord>(pack.Stages.Count);
            foreach (AuthoredStage src in pack.Stages)
            {
                var phases = new List<StagePhaseRecord>(src.Phases.Count);
                foreach (AuthoredPhase p in src.Phases)
                {
                    ContentValidator.TryParseCarryItems(p.CarryItems, out CarryItemsMode carryItems);
                    ContentValidator.TryParseCarryJewels(p.CarryJewels, out CarryJewelsMode carryJewels);
                    long dropRaw = DecimalFixParser.ParseToRaw(p.CarryJewelsDropChance);
                    int dropPermille = (int)((dropRaw * 1000) >> DecimalFixParser.FractionBits);
                    if (dropPermille < 0)
                    {
                        dropPermille = 0;
                    }

                    if (dropPermille > 1000)
                    {
                        dropPermille = 1000;
                    }

                    var hazards = new List<StageHazardRecord>(p.Hazards.Count);
                    foreach (AuthoredHazard h in p.Hazards)
                    {
                        ContentValidator.TryParseHazardTrigger(h.Trigger, out HazardTriggerKind trigger);
                        hazards.Add(new StageHazardRecord
                        {
                            HazardId = h.HazardId,
                            Name = h.Name,
                            Trigger = trigger,
                            PeriodFrames = SecondsRawToFrames(DecimalFixParser.ParseToRaw(h.PeriodSeconds)),
                            TelegraphFrames = h.TelegraphFrames < 0 ? 0 : h.TelegraphFrames,
                            Damage = h.Damage,
                            CenterXRaw = DecimalFixParser.ParseToRaw(h.CenterX),
                            CenterYRaw = DecimalFixParser.ParseToRaw(h.CenterY),
                            CenterZRaw = DecimalFixParser.ParseToRaw(h.CenterZ),
                            RadiusRaw = DecimalFixParser.ParseToRaw(h.Radius),
                        });
                    }

                    var kinematics = new List<StageKinematicRecord>(p.Kinematics.Count);
                    foreach (AuthoredKinematic k in p.Kinematics)
                    {
                        kinematics.Add(new StageKinematicRecord
                        {
                            TrackId = k.TrackId,
                            Name = k.Name,
                            PeriodFrames = SecondsRawToFrames(DecimalFixParser.ParseToRaw(k.PeriodSeconds)),
                            AmplitudeRaw = DecimalFixParser.ParseToRaw(k.Amplitude),
                            OriginXRaw = DecimalFixParser.ParseToRaw(k.OriginX),
                            OriginYRaw = DecimalFixParser.ParseToRaw(k.OriginY),
                            OriginZRaw = DecimalFixParser.ParseToRaw(k.OriginZ),
                        });
                    }

                    StagePhaseRecord phaseRec = new StagePhaseRecord
                    {
                        Name = p.Name,
                        DurationFrames = SecondsRawToFrames(DecimalFixParser.ParseToRaw(p.DurationSeconds)),
                        GeometrySetId = p.GeometrySetId,
                        BlitzSuppressed = p.BlitzSuppressed,
                        TransitionFrames = SecondsRawToFrames(DecimalFixParser.ParseToRaw(p.TransitionSeconds)),
                        CarryItems = carryItems,
                        CarryJewels = carryJewels,
                        CarryJewelsDropPermille = dropPermille,
                        CameraCenterXRaw = DecimalFixParser.ParseToRaw(p.CameraCenterX),
                        CameraCenterYRaw = DecimalFixParser.ParseToRaw(p.CameraCenterY),
                        CameraCenterZRaw = DecimalFixParser.ParseToRaw(p.CameraCenterZ),
                        CameraRadiusRaw = DecimalFixParser.ParseToRaw(p.CameraRadius),
                        Hazards = hazards.ToArray(),
                        Kinematics = kinematics.ToArray(),
                    };
                    ApplyScriptedCamera(p.ScriptedCamera, phaseRec);
                    phases.Add(phaseRec);
                }

                stages.Add(new StageRecord
                {
                    StageId = src.StageId,
                    Name = src.Name,
                    JewelCrateChancePercent = src.JewelCrateChancePercent,
                    FallIsKo = src.FallIsKo,
                    JewelCapOverride = src.JewelCapOverride,
                    Phases = phases.ToArray(),
                });
            }

            var items = new List<ItemRecord>(pack.Items.Count);
            foreach (AuthoredItem src in pack.Items)
            {
                ContentValidator.TryParseCategory(src.Category, out ItemCategory category);
                ContentValidator.TryParseEligibility(src.CrateEligibility, out CrateEligibility eligibility);
                items.Add(new ItemRecord
                {
                    ItemId = src.ItemId,
                    Name = src.Name,
                    Category = category,
                    Rarity = src.Rarity,
                    CrateEligibility = eligibility,
                    Pocketable = src.Pocketable,
                    BuyPrice = src.BuyPrice,
                    SellPrice = src.SellPrice,
                    DamagePerUse = src.DamagePerUse,
                    HealPerUse = src.HealPerUse,
                });
            }

            return new ContentCatalog(fighters.ToArray(), stages.ToArray(), items.ToArray());
        }

        /// <summary>
        /// Power scale 0.85/0.92/1.0/1.08/1.15 for ratings 1–5 (balance.md §3.1).
        /// Applied at export with integer half-up: (base * percent + 50) / 100.
        /// </summary>
        public static int ApplyPowerScale(int baseDamage, MoveClass moveClass, int powerRating, bool utilityExempt)
        {
            if (utilityExempt || moveClass == MoveClass.UtilityStrong || moveClass == MoveClass.Flare || moveClass == MoveClass.Surge)
            {
                return baseDamage;
            }

            int percent;
            switch (powerRating)
            {
                case 1: percent = 85; break;
                case 2: percent = 92; break;
                case 3: percent = 100; break;
                case 4: percent = 108; break;
                case 5: percent = 115; break;
                default: percent = 100; break;
            }

            return (baseDamage * percent + 50) / 100;
        }

        public static int SecondsRawToFrames(long secondsRaw)
        {
            // frames = seconds * 60, Q48.16 → (secondsRaw * 60) >> 16
            return (int)((secondsRaw * TickRateHz) >> DecimalFixParser.FractionBits);
        }

        private static void ApplyScriptedCamera(AuthoredScriptedCamera? authored, StagePhaseRecord phase)
        {
            if (authored == null || !authored.Enabled)
            {
                phase.HasScriptedCamera = false;
                phase.ScriptedCameraMask = 0;
                return;
            }

            byte mask = 0;
            if (!string.IsNullOrWhiteSpace(authored.StartCenterX))
            {
                phase.ScriptedStartCenterXRaw = DecimalFixParser.ParseToRaw(authored.StartCenterX);
                mask |= ScriptedCameraMaskBits.StartX;
            }

            if (!string.IsNullOrWhiteSpace(authored.StartCenterY))
            {
                phase.ScriptedStartCenterYRaw = DecimalFixParser.ParseToRaw(authored.StartCenterY);
                mask |= ScriptedCameraMaskBits.StartY;
            }

            if (!string.IsNullOrWhiteSpace(authored.StartCenterZ))
            {
                phase.ScriptedStartCenterZRaw = DecimalFixParser.ParseToRaw(authored.StartCenterZ);
                mask |= ScriptedCameraMaskBits.StartZ;
            }

            if (!string.IsNullOrWhiteSpace(authored.StartRadius))
            {
                phase.ScriptedStartRadiusRaw = DecimalFixParser.ParseToRaw(authored.StartRadius);
                mask |= ScriptedCameraMaskBits.StartR;
            }

            if (!string.IsNullOrWhiteSpace(authored.EndCenterX))
            {
                phase.ScriptedEndCenterXRaw = DecimalFixParser.ParseToRaw(authored.EndCenterX);
                mask |= ScriptedCameraMaskBits.EndX;
            }

            if (!string.IsNullOrWhiteSpace(authored.EndCenterY))
            {
                phase.ScriptedEndCenterYRaw = DecimalFixParser.ParseToRaw(authored.EndCenterY);
                mask |= ScriptedCameraMaskBits.EndY;
            }

            if (!string.IsNullOrWhiteSpace(authored.EndCenterZ))
            {
                phase.ScriptedEndCenterZRaw = DecimalFixParser.ParseToRaw(authored.EndCenterZ);
                mask |= ScriptedCameraMaskBits.EndZ;
            }

            if (!string.IsNullOrWhiteSpace(authored.EndRadius))
            {
                phase.ScriptedEndRadiusRaw = DecimalFixParser.ParseToRaw(authored.EndRadius);
                mask |= ScriptedCameraMaskBits.EndR;
            }

            phase.HasScriptedCamera = true;
            phase.ScriptedCameraMask = mask;
        }
    }
}
