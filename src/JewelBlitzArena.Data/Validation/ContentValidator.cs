using System;
using System.Collections.Generic;

namespace JewelBlitzArena.Data
{
    public sealed class ValidationIssue
    {
        public ValidationIssue(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }

        public override string ToString() => $"{Code}: {Message}";
    }

    public sealed class ValidationResult
    {
        public List<ValidationIssue> Errors { get; } = new List<ValidationIssue>();
        public List<ValidationIssue> Warnings { get; } = new List<ValidationIssue>();
        public bool IsOk => Errors.Count == 0;
    }

    /// <summary>Validates authored packs against registry uniqueness and balance.md budgets.</summary>
    public static class ContentValidator
    {
        public static ValidationResult Validate(AuthoredContentPack pack)
        {
            var result = new ValidationResult();
            if (pack.SchemaVersion != ContentSchema.Version)
            {
                result.Errors.Add(new ValidationIssue(
                    "schema_version",
                    $"Expected schema {ContentSchema.Version}, got {pack.SchemaVersion}."));
            }

            var fighterIds = new HashSet<int>();
            var stageIds = new HashSet<int>();
            var itemIds = new HashSet<int>();

            foreach (AuthoredFighter fighter in pack.Fighters)
            {
                if (!fighterIds.Add(fighter.FighterId))
                {
                    result.Errors.Add(new ValidationIssue("fighter_id", $"Duplicate fighter_id {fighter.FighterId}."));
                }

                int expectedLife = BalanceBudgets.MaxLifeForRating(fighter.LifeRating);
                if (expectedLife < 0)
                {
                    result.Errors.Add(new ValidationIssue(
                        "life_rating",
                        $"Fighter '{fighter.Name}' has invalid Life rating {fighter.LifeRating}."));
                }

                foreach (AuthoredMove move in fighter.Moves)
                {
                    if (!TryParseMoveClass(move.Class, out MoveClass moveClass))
                    {
                        result.Errors.Add(new ValidationIssue(
                            "move_class",
                            $"Fighter '{fighter.Name}' move '{move.Name}' has unknown class '{move.Class}'."));
                        continue;
                    }

                    if (moveClass == MoveClass.UtilityStrong || move.UtilityExempt)
                    {
                        if (move.Damage != 0 && moveClass == MoveClass.UtilityStrong)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "utility_strong",
                                $"Fighter '{fighter.Name}' utility Strong '{move.Name}' must deal 0 damage."));
                        }

                        continue;
                    }

                    if (!BalanceBudgets.TryGetDamageBand(moveClass, out int min, out int max))
                    {
                        continue;
                    }

                    // Power scale is applied at export; authored base kits are validated pre-scale for S5 stubs.
                    if (move.Damage < min || move.Damage > max)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "damage_budget",
                            $"Fighter '{fighter.Name}' move '{move.Name}' damage {move.Damage} outside {min}-{max} for {moveClass}."));
                    }
                }

                try
                {
                    _ = DecimalFixParser.ParseToRaw(fighter.WalkSpeed);
                    _ = DecimalFixParser.ParseToRaw(fighter.RunSpeed);
                    _ = DecimalFixParser.ParseToRaw(fighter.FullSpeed);
                }
                catch (FormatException ex)
                {
                    result.Errors.Add(new ValidationIssue("fix_speed", $"Fighter '{fighter.Name}': {ex.Message}"));
                }
            }

            foreach (AuthoredStage stage in pack.Stages)
            {
                if (!stageIds.Add(stage.StageId))
                {
                    result.Errors.Add(new ValidationIssue("stage_id", $"Duplicate stage_id {stage.StageId}."));
                }

                if (stage.Phases.Count == 0)
                {
                    result.Errors.Add(new ValidationIssue("stage_phases", $"Stage '{stage.Name}' has no phases."));
                }

                foreach (AuthoredPhase phase in stage.Phases)
                {
                    try
                    {
                        _ = DecimalFixParser.ParseToRaw(phase.DurationSeconds);
                    }
                    catch (FormatException ex)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_duration",
                            $"Stage '{stage.Name}' phase '{phase.Name}': {ex.Message}"));
                    }
                }
            }

            int alwaysCount = 0;
            foreach (AuthoredItem item in pack.Items)
            {
                if (!itemIds.Add(item.ItemId))
                {
                    result.Errors.Add(new ValidationIssue("item_id", $"Duplicate item_id {item.ItemId}."));
                }

                if (!BalanceBudgets.IsValidItemRarity(item.Rarity))
                {
                    result.Errors.Add(new ValidationIssue(
                        "item_rarity",
                        $"Item '{item.Name}' rarity {item.Rarity} must be 1..7."));
                }

                if (!TryParseCategory(item.Category, out _))
                {
                    result.Errors.Add(new ValidationIssue(
                        "item_category",
                        $"Item '{item.Name}' has unknown category '{item.Category}'."));
                }

                if (!TryParseEligibility(item.CrateEligibility, out CrateEligibility eligibility))
                {
                    result.Errors.Add(new ValidationIssue(
                        "crate_eligibility",
                        $"Item '{item.Name}' has unknown crate_eligibility '{item.CrateEligibility}'."));
                }
                else if (eligibility == CrateEligibility.Always)
                {
                    alwaysCount++;
                }

                int maxDmg = BalanceBudgets.MaxItemDamagePerUse(item.Rarity);
                if (item.DamagePerUse > maxDmg)
                {
                    result.Errors.Add(new ValidationIssue(
                        "item_damage",
                        $"Item '{item.Name}' damage {item.DamagePerUse} exceeds rarity-{item.Rarity} ceiling {maxDmg}."));
                }

                // No item KOs from full Life (proxy: damage < lowest max Life).
                if (item.DamagePerUse >= 850)
                {
                    result.Errors.Add(new ValidationIssue(
                        "item_ko",
                        $"Item '{item.Name}' damage {item.DamagePerUse} can KO from full Life."));
                }
            }

            if (alwaysCount > 0 && alwaysCount < 25)
            {
                result.Warnings.Add(new ValidationIssue(
                    "starter_set",
                    $"Starter set has {alwaysCount} always-eligible items; design calls for 25."));
            }

            return result;
        }

        public static bool TryParseMoveClass(string text, out MoveClass value)
        {
            return Enum.TryParse(text, ignoreCase: true, out value);
        }

        public static bool TryParseCategory(string text, out ItemCategory value)
        {
            return Enum.TryParse(text, ignoreCase: true, out value);
        }

        public static bool TryParseEligibility(string text, out CrateEligibility value)
        {
            return Enum.TryParse(text, ignoreCase: true, out value);
        }
    }
}
