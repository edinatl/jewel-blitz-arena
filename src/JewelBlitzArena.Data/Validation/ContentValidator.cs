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

                if (!TryParseKitBehavior(fighter.KitBehavior, out _))
                {
                    result.Errors.Add(new ValidationIssue(
                        "kit_behavior",
                        $"Fighter '{fighter.Name}' has unknown kitBehavior '{fighter.KitBehavior}'."));
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

                    if (!TryParseMoveEffect(move.Effect, out MoveEffect effect))
                    {
                        result.Errors.Add(new ValidationIssue(
                            "move_effect",
                            $"Fighter '{fighter.Name}' move '{move.Name}' has unknown effect '{move.Effect}'."));
                        continue;
                    }

                    if (effect == MoveEffect.Heal && move.Damage <= 0)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "move_effect",
                            $"Fighter '{fighter.Name}' heal move '{move.Name}' must have positive heal amount."));
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
                        _ = DecimalFixParser.ParseToRaw(phase.TransitionSeconds);
                        _ = DecimalFixParser.ParseToRaw(phase.CarryJewelsDropChance);
                        _ = DecimalFixParser.ParseToRaw(phase.CameraCenterX);
                        _ = DecimalFixParser.ParseToRaw(phase.CameraCenterY);
                        _ = DecimalFixParser.ParseToRaw(phase.CameraCenterZ);
                        _ = DecimalFixParser.ParseToRaw(phase.CameraRadius);
                        ValidateOptionalScriptedCamera(stage.Name, phase, result);
                    }
                    catch (FormatException ex)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_duration",
                            $"Stage '{stage.Name}' phase '{phase.Name}': {ex.Message}"));
                    }

                    if (!TryParseCarryItems(phase.CarryItems, out _))
                    {
                        result.Errors.Add(new ValidationIssue(
                            "carry_items",
                            $"Stage '{stage.Name}' phase '{phase.Name}' carryItems '{phase.CarryItems}' invalid."));
                    }

                    if (!TryParseCarryJewels(phase.CarryJewels, out _))
                    {
                        result.Errors.Add(new ValidationIssue(
                            "carry_jewels",
                            $"Stage '{stage.Name}' phase '{phase.Name}' carryJewels '{phase.CarryJewels}' invalid."));
                    }

                    if (phase.Hazards.Count > ContentSchema.MaxHazardsPerPhase)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_hazards",
                            $"Stage '{stage.Name}' phase '{phase.Name}' exceeds hazard cap."));
                    }

                    if (phase.Kinematics.Count > ContentSchema.MaxKinematicsPerPhase)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_kinematics",
                            $"Stage '{stage.Name}' phase '{phase.Name}' exceeds kinematic cap."));
                    }

                    if (phase.CratePoints.Count > ContentSchema.MaxCratePointsPerPhase)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_crate_points",
                            $"Stage '{stage.Name}' phase '{phase.Name}' exceeds crate point cap."));
                    }

                    if (phase.WaterVolumes.Count > ContentSchema.MaxWaterVolumesPerPhase)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_water_volumes",
                            $"Stage '{stage.Name}' phase '{phase.Name}' exceeds water volume cap."));
                    }

                    if (phase.Creatures.Count > ContentSchema.MaxCreaturesPerPhase)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "phase_creatures",
                            $"Stage '{stage.Name}' phase '{phase.Name}' exceeds creature cap."));
                    }

                    foreach (AuthoredHazard hazard in phase.Hazards)
                    {
                        if (!TryParseHazardTrigger(hazard.Trigger, out _))
                        {
                            result.Errors.Add(new ValidationIssue(
                                "hazard_trigger",
                                $"Hazard '{hazard.Name}' trigger '{hazard.Trigger}' invalid."));
                        }

                        try
                        {
                            _ = DecimalFixParser.ParseToRaw(hazard.PeriodSeconds);
                            _ = DecimalFixParser.ParseToRaw(hazard.Radius);
                        }
                        catch (FormatException ex)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "hazard_fix",
                                $"Hazard '{hazard.Name}': {ex.Message}"));
                        }
                    }

                    foreach (AuthoredKinematic track in phase.Kinematics)
                    {
                        try
                        {
                            _ = DecimalFixParser.ParseToRaw(track.PeriodSeconds);
                            _ = DecimalFixParser.ParseToRaw(track.Amplitude);
                        }
                        catch (FormatException ex)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "kinematic_fix",
                                $"Kinematic '{track.Name}': {ex.Message}"));
                        }
                    }

                    for (int ci = 0; ci < phase.CratePoints.Count; ci++)
                    {
                        AuthoredCratePoint crate = phase.CratePoints[ci];
                        try
                        {
                            _ = DecimalFixParser.ParseToRaw(crate.X);
                            _ = DecimalFixParser.ParseToRaw(crate.Z);
                        }
                        catch (FormatException ex)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "crate_point_fix",
                                $"Stage '{stage.Name}' phase '{phase.Name}' crate[{ci}]: {ex.Message}"));
                        }
                    }

                    for (int wi = 0; wi < phase.WaterVolumes.Count; wi++)
                    {
                        AuthoredWaterVolume water = phase.WaterVolumes[wi];
                        try
                        {
                            _ = DecimalFixParser.ParseToRaw(water.CenterX);
                            _ = DecimalFixParser.ParseToRaw(water.CenterZ);
                            _ = DecimalFixParser.ParseToRaw(water.Radius);
                        }
                        catch (FormatException ex)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "water_volume_fix",
                                $"Stage '{stage.Name}' phase '{phase.Name}' water[{wi}]: {ex.Message}"));
                        }
                    }

                    for (int gi = 0; gi < phase.Creatures.Count; gi++)
                    {
                        AuthoredCreature creature = phase.Creatures[gi];
                        if (creature.KindId < 1 || creature.KindId > 5)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "creature_kind",
                                $"Stage '{stage.Name}' phase '{phase.Name}' creature[{gi}] kind {creature.KindId} out of 1–5."));
                        }

                        if (creature.MaxLife < 0)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "creature_life",
                                $"Stage '{stage.Name}' phase '{phase.Name}' creature[{gi}] maxLife must be ≥ 0."));
                        }

                        try
                        {
                            _ = DecimalFixParser.ParseToRaw(creature.X);
                            _ = DecimalFixParser.ParseToRaw(creature.Z);
                        }
                        catch (FormatException ex)
                        {
                            result.Errors.Add(new ValidationIssue(
                                "creature_fix",
                                $"Stage '{stage.Name}' phase '{phase.Name}' creature[{gi}]: {ex.Message}"));
                        }
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

                if (item.Tags != null)
                {
                    if (item.Tags.Count > ContentSchema.MaxTagsPerItem)
                    {
                        result.Errors.Add(new ValidationIssue(
                            "item_tags",
                            $"Item '{item.Name}' has {item.Tags.Count} tags; max {ContentSchema.MaxTagsPerItem}."));
                    }

                    foreach (string tag in item.Tags)
                    {
                        if (!TryParseTag(tag, out _))
                        {
                            result.Errors.Add(new ValidationIssue(
                                "item_tags",
                                $"Item '{item.Name}' has unknown tag '{tag}'."));
                        }
                    }
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

        public static bool TryParseMoveEffect(string? text, out MoveEffect value)
        {
            value = MoveEffect.None;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            return Enum.TryParse(text.Trim(), ignoreCase: true, out value);
        }

        /// <summary>
        /// Parses authored kit-behavior names into Sim ordinals (architecture §2.5).
        /// Kept as byte here so Data stays free of a Sim enum reference.
        /// </summary>
        public static bool TryParseKitBehavior(string? text, out byte value)
        {
            value = 0; // None
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            switch (text.Trim().ToLowerInvariant())
            {
                case "none":
                    value = 0;
                    return true;
                case "vanishstrong":
                case "vanish_strong":
                    value = 1;
                    return true;
                case "commandgrabcarry":
                case "command_grab_carry":
                    value = 2;
                    return true;
                case "ripostestrong":
                case "riposte_strong":
                    value = 3;
                    return true;
                case "extendeddodge":
                case "extended_dodge":
                    value = 4;
                    return true;
                case "triplejumpglide":
                case "triple_jump_glide":
                    value = 5;
                    return true;
                case "doublehold":
                case "double_hold":
                    value = 6;
                    return true;
                case "swappartners":
                case "swap_partners":
                    value = 7;
                    return true;
                case "invoicerepeatable":
                case "invoice_repeatable":
                    value = 8;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryParseCategory(string text, out ItemCategory value)
        {
            return Enum.TryParse(text, ignoreCase: true, out value);
        }

        public static bool TryParseEligibility(string text, out CrateEligibility value)
        {
            return Enum.TryParse(text, ignoreCase: true, out value);
        }

        public static bool TryParseTag(string text, out ItemTag value)
        {
            value = ItemTag.None;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return Enum.TryParse(text.Trim(), ignoreCase: true, out value)
                && value != ItemTag.None
                && ((ushort)value & ((ushort)value - 1)) == 0; // single bit
        }

        public static ItemTag ParseTagMask(IEnumerable<string>? tags)
        {
            ItemTag mask = ItemTag.None;
            if (tags == null)
            {
                return mask;
            }

            foreach (string tag in tags)
            {
                if (TryParseTag(tag, out ItemTag bit))
                {
                    mask |= bit;
                }
            }

            return mask;
        }

        public static bool TryParseCarryItems(string text, out CarryItemsMode value)
        {
            value = CarryItemsMode.All;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            switch (text.Trim().ToLowerInvariant())
            {
                case "all":
                    value = CarryItemsMode.All;
                    return true;
                case "held_only":
                case "heldonly":
                    value = CarryItemsMode.HeldOnly;
                    return true;
                case "none":
                    value = CarryItemsMode.None;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryParseCarryJewels(string text, out CarryJewelsMode value)
        {
            value = CarryJewelsMode.All;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            switch (text.Trim().ToLowerInvariant())
            {
                case "all":
                    value = CarryJewelsMode.All;
                    return true;
                case "drop_chance":
                case "dropchance":
                    value = CarryJewelsMode.DropChance;
                    return true;
                case "none":
                    value = CarryJewelsMode.None;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryParseHazardTrigger(string text, out HazardTriggerKind value)
        {
            value = HazardTriggerKind.Timer;
            if (string.IsNullOrWhiteSpace(text))
            {
                return true;
            }

            switch (text.Trim().ToLowerInvariant())
            {
                case "phase_enter":
                case "phaseenter":
                    value = HazardTriggerKind.PhaseEnter;
                    return true;
                case "timer":
                    value = HazardTriggerKind.Timer;
                    return true;
                case "proximity":
                    value = HazardTriggerKind.Proximity;
                    return true;
                default:
                    return false;
            }
        }

        private static void ValidateOptionalScriptedCamera(
            string stageName,
            AuthoredPhase phase,
            ValidationResult result)
        {
            AuthoredScriptedCamera? cam = phase.ScriptedCamera;
            if (cam == null)
            {
                return;
            }

            TryParseOptionalFix(cam.StartCenterX, stageName, phase.Name, "scripted_camera.startCenterX", result);
            TryParseOptionalFix(cam.StartCenterY, stageName, phase.Name, "scripted_camera.startCenterY", result);
            TryParseOptionalFix(cam.StartCenterZ, stageName, phase.Name, "scripted_camera.startCenterZ", result);
            TryParseOptionalFix(cam.StartRadius, stageName, phase.Name, "scripted_camera.startRadius", result);
            TryParseOptionalFix(cam.EndCenterX, stageName, phase.Name, "scripted_camera.endCenterX", result);
            TryParseOptionalFix(cam.EndCenterY, stageName, phase.Name, "scripted_camera.endCenterY", result);
            TryParseOptionalFix(cam.EndCenterZ, stageName, phase.Name, "scripted_camera.endCenterZ", result);
            TryParseOptionalFix(cam.EndRadius, stageName, phase.Name, "scripted_camera.endRadius", result);
        }

        private static void TryParseOptionalFix(
            string? value,
            string stageName,
            string phaseName,
            string field,
            ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                _ = DecimalFixParser.ParseToRaw(value);
            }
            catch (FormatException ex)
            {
                result.Errors.Add(new ValidationIssue(
                    field,
                    $"Stage '{stageName}' phase '{phaseName}' {field}: {ex.Message}"));
            }
        }
    }
}
