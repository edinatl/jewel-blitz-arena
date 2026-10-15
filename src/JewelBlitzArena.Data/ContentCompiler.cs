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
                    Moves = moves.ToArray(),
                });
            }

            var stages = new List<StageRecord>(pack.Stages.Count);
            foreach (AuthoredStage src in pack.Stages)
            {
                var phases = new List<StagePhaseRecord>(src.Phases.Count);
                foreach (AuthoredPhase p in src.Phases)
                {
                    long secondsRaw = DecimalFixParser.ParseToRaw(p.DurationSeconds);
                    int durationFrames = SecondsRawToFrames(secondsRaw);
                    phases.Add(new StagePhaseRecord
                    {
                        Name = p.Name,
                        DurationFrames = durationFrames,
                    });
                }

                stages.Add(new StageRecord
                {
                    StageId = src.StageId,
                    Name = src.Name,
                    JewelCrateChancePercent = src.JewelCrateChancePercent,
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
    }
}
