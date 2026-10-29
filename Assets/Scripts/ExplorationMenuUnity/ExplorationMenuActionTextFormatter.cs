using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.PlayerState;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    internal static class ExplorationMenuActionTextFormatter
    {
        internal static string FormatActionLibraryFilter(CompatibleWeaponTypes filter)
        {
            switch (filter)
            {
                case CompatibleWeaponTypes.All: return "Common";
                case CompatibleWeaponTypes.Greatsword: return "Greatsword";
                case CompatibleWeaponTypes.SwordAndShield: return "Sword & Shield";
                case CompatibleWeaponTypes.GlaiveAndShield: return "Glaive & Shield";
                case CompatibleWeaponTypes.Bow: return "Bow";
                case CompatibleWeaponTypes.Bladeguard: return "Bladeguard";
                default: return filter.ToString();
            }
        }

        internal static string FormatActionSlot(PreparedComboActionSlot slot)
        {
            switch (slot)
            {
                case PreparedComboActionSlot.Opening: return "Opening [O]";
                case PreparedComboActionSlot.Main: return "Main [M]";
                case PreparedComboActionSlot.Closing: return "Closing [C]";
                case PreparedComboActionSlot.Linked: return "Linked [L]";
                default: return slot.ToString();
            }
        }

        internal static string FormatActionLibrarySummary(ComboActionDefinition action)
        {
            var lines = new List<string>();
            for (int index = 0; index < action.ResolutionEvents.Count; index++)
            {
                ActionResolutionDefinition resolution = action.ResolutionEvents[index];
                if (resolution.Effect == ActionResolutionEffect.None
                    && resolution.ItemCost == null)
                {
                    continue;
                }

                string timing;
                if (resolution.HasResolutionWindow)
                {
                    timing = $"T{resolution.ResolutionWindowStart.Value}–T{resolution.ResolutionWindowEnd.Value}";
                }
                else if (action.IsChargeable
                    && index == 0
                    && resolution.Offset == action.ResolutionOffset)
                {
                    timing = $"T{resolution.Offset}";
                    for (int level = 0; level < action.ChargeLevels.Count; level++)
                    {
                        timing += $"/T{resolution.Offset + action.ChargeLevels[level].AdditionalTimelineCost}";
                    }
                }
                else
                {
                    timing = $"T{resolution.Offset}";
                }

                string detail = FormatResolutionSummary(resolution);
                lines.Add($"• {timing} — {detail}");
            }

            if (action.IsChargeable)
            {
                lines.Add(
                    $"• Charge Levels 0–{action.ChargeLevels.Count}; later levels extend the Timeline");
            }

            for (int index = 0; index < action.ActiveWindows.Count; index++)
            {
                ActionActiveWindowDefinition window = action.ActiveWindows[index];
                string effect = window.Effect == ActiveWindowEffect.SuperArmor
                    ? $"Super Armor {ToRomanNumeral(window.SuperArmorLevel)}"
                    : $"Damage Taken -{window.DamageReduction * 100d:0}%";
                lines.Add($"• T{window.StartOffset}–T{window.EndOffset} — {effect}");
            }

            if (action.SustainedAttack != null)
            {
                lines.Add(
                    $"• Sustained Attack T{action.SustainedAttack.WindowStartOffset}–T{action.SustainedAttack.WindowEndOffset}"
                    + $"; every {action.SustainedAttack.HitInterval} Time Steps"
                    + $"; up to {action.SustainedAttack.MaximumHitCount} hits");
            }

            if (action.PreviousActionStatusRequirement != null)
            {
                lines.Add(
                    $"• Requires previous Action: {action.PreviousActionStatusRequirement.DisplayName}");
            }

            return lines.Count == 0 ? "• No Resolution Events" : string.Join("\n", lines);
        }

        internal static string FormatActionSequence(ComboConfiguration configuration)
        {
            IReadOnlyList<ComboActionDefinition> actions = configuration.BuildExecutionOrder();
            string result = string.Empty;
            for (int index = 0; index < actions.Count; index++)
            {
                if (index > 0)
                {
                    result += "  →  ";
                }

                result += actions[index].DisplayName;
            }

            return result;
        }

        internal static string FormatFirstIssue(ComboValidationResult validation)
        {
            if (validation == null || validation.IsValid || validation.Issues.Count == 0)
            {
                return string.Empty;
            }

            ComboValidationIssue issue = validation.Issues[0];
            return $"{issue.Code} in {issue.ComboName}.";
        }

        internal static string ShortPlacementName(PreparedLinkedActionPlacement placement)
        {
            switch (placement)
            {
                case PreparedLinkedActionPlacement.AfterOpening: return "After O";
                case PreparedLinkedActionPlacement.AfterMain: return "After M";
                case PreparedLinkedActionPlacement.AfterClosing: return "After C";
                default: return placement.ToString();
            }
        }

        private static string FormatResolutionSummary(ActionResolutionDefinition resolution)
        {
            string result = resolution.HasResolutionWindow
                ? $"{resolution.Effect} Resolution Window"
                : resolution.Effect.ToString();
            if (resolution.DamageEffect != null)
            {
                result = "Damage: ";
                for (int index = 0; index < resolution.DamageEffect.Components.Count; index++)
                {
                    if (index > 0)
                    {
                        result += ", ";
                    }

                    var component = resolution.DamageEffect.Components[index];
                    result += $"{component.Type} {component.ActionMultiplier * 100d:0}%";
                }

                if (resolution.DamageTags != DamageResolutionTag.None)
                {
                    result += $" [{resolution.DamageTags}]";
                }

                if (resolution.OnHitDisruption != null)
                {
                    result += $"; {resolution.OnHitDisruption.Type} {resolution.OnHitDisruption.Duration}";
                }
            }
            else if (resolution.Healing != null)
            {
                result = resolution.Healing.Type == ActionHealingType.FlatAmount
                    ? $"Heal {resolution.Healing.Value:0} HP"
                    : $"Heal {resolution.Healing.Value * 100d:0}% Max HP";
            }
            else if (resolution.StaminaRecovery > 0)
            {
                result = $"Restore {resolution.StaminaRecovery} Stamina";
            }
            else if (resolution.StatusEffect != null)
            {
                string target = resolution.StatusTarget == ActionStatusTarget.Self
                    ? "Self" : "Opponent";
                string durationUnit = resolution.StatusEffect.DurationType
                    == CombatStatusDurationType.Rounds ? "Rounds" : "Time Steps";
                result = $"{resolution.Effect}: {resolution.StatusEffect.DisplayName} ({target})"
                    + $" for {resolution.StatusEffect.Duration} {durationUnit}";
            }

            if (resolution.ItemCost != null)
            {
                result += $"; consumes {resolution.ItemCost.Amount} {resolution.ItemCost.DisplayName}";
            }

            return result;
        }

        private static string ToRomanNumeral(int value)
        {
            switch (value)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                default: return value.ToString();
            }
        }
    }
}
