using System;
using System.Collections.Generic;

namespace SER494.Gameplay.Combat.SoloCombo
{
    public enum ComboValidationErrorCode
    {
        EmptyLoadout,
        NullCombo,
        MissingMainAction,
        InvalidOpeningActionCategory,
        InvalidMainActionCategory,
        InvalidClosingActionCategory,
        InvalidLinkedActionCategory,
        MissingLinkedActionPlacement,
        LinkedActionPlacementWithoutLinkedAction,
        LinkedActionPlacementHasNoAction,
        LoadCostExceeded,
        ManaCostExceeded,
        StaminaCostExceeded,
        ActionAssignedMoreThanOnce
    }

    public sealed class ComboValidationLimits
    {
        public ComboValidationLimits(int maximumLoad, int maximumMana, int maximumStamina)
        {
            MaximumLoad = RequireNonNegative(maximumLoad, nameof(maximumLoad));
            MaximumMana = RequireNonNegative(maximumMana, nameof(maximumMana));
            MaximumStamina = RequireNonNegative(maximumStamina, nameof(maximumStamina));
        }

        public int MaximumLoad { get; }

        public int MaximumMana { get; }

        public int MaximumStamina { get; }

        private static int RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value cannot be negative.");
            }

            return value;
        }
    }

    public sealed class ComboValidationIssue
    {
        public ComboValidationIssue(
            ComboValidationErrorCode code,
            string comboName,
            string actionId = null)
        {
            Code = code;
            ComboName = comboName;
            ActionId = actionId;
        }

        public ComboValidationErrorCode Code { get; }

        public string ComboName { get; }

        public string ActionId { get; }
    }

    public sealed class ComboValidationResult
    {
        private readonly List<ComboValidationIssue> _issues = new List<ComboValidationIssue>();

        public bool IsValid => _issues.Count == 0;

        public IReadOnlyList<ComboValidationIssue> Issues => _issues;

        internal void Add(ComboValidationErrorCode code, string comboName, string actionId = null)
        {
            _issues.Add(new ComboValidationIssue(code, comboName, actionId));
        }

        internal void AddRange(IReadOnlyList<ComboValidationIssue> issues)
        {
            for (int index = 0; index < issues.Count; index++)
            {
                _issues.Add(issues[index]);
            }
        }
    }

    public static class ComboValidator
    {
        public static ComboValidationResult Validate(
            ComboConfiguration combo,
            ComboValidationLimits limits)
        {
            if (combo == null)
            {
                throw new ArgumentNullException(nameof(combo));
            }

            if (limits == null)
            {
                throw new ArgumentNullException(nameof(limits));
            }

            var result = new ComboValidationResult();
            ValidateActionSlots(combo, result);
            ValidateLinkedAction(combo, result);
            ValidateCosts(combo, limits, result);
            return result;
        }

        private static void ValidateActionSlots(
            ComboConfiguration combo,
            ComboValidationResult result)
        {
            if (combo.OpeningAction != null
                && combo.OpeningAction.Category != ComboActionCategory.Opening)
            {
                result.Add(
                    ComboValidationErrorCode.InvalidOpeningActionCategory,
                    combo.Name,
                    combo.OpeningAction.ActionId);
            }

            if (combo.MainAction == null)
            {
                result.Add(ComboValidationErrorCode.MissingMainAction, combo.Name);
            }
            else if (combo.MainAction.Category != ComboActionCategory.Main)
            {
                result.Add(
                    ComboValidationErrorCode.InvalidMainActionCategory,
                    combo.Name,
                    combo.MainAction.ActionId);
            }

            if (combo.ClosingAction != null
                && combo.ClosingAction.Category != ComboActionCategory.Closing)
            {
                result.Add(
                    ComboValidationErrorCode.InvalidClosingActionCategory,
                    combo.Name,
                    combo.ClosingAction.ActionId);
            }

            if (combo.LinkedAction != null
                && combo.LinkedAction.Category != ComboActionCategory.Linked)
            {
                result.Add(
                    ComboValidationErrorCode.InvalidLinkedActionCategory,
                    combo.Name,
                    combo.LinkedAction.ActionId);
            }
        }

        private static void ValidateLinkedAction(
            ComboConfiguration combo,
            ComboValidationResult result)
        {
            if (combo.LinkedAction == null)
            {
                if (combo.LinkedPlacement.HasValue)
                {
                    result.Add(
                        ComboValidationErrorCode.LinkedActionPlacementWithoutLinkedAction,
                        combo.Name);
                }

                return;
            }

            if (!combo.LinkedPlacement.HasValue)
            {
                result.Add(
                    ComboValidationErrorCode.MissingLinkedActionPlacement,
                    combo.Name,
                    combo.LinkedAction.ActionId);
                return;
            }

            bool targetExists;
            switch (combo.LinkedPlacement.Value)
            {
                case LinkedActionPlacement.AfterOpening:
                    targetExists = combo.OpeningAction != null;
                    break;
                case LinkedActionPlacement.AfterMain:
                    targetExists = combo.MainAction != null;
                    break;
                case LinkedActionPlacement.AfterClosing:
                    targetExists = combo.ClosingAction != null;
                    break;
                default:
                    targetExists = false;
                    break;
            }

            if (!targetExists)
            {
                result.Add(
                    ComboValidationErrorCode.LinkedActionPlacementHasNoAction,
                    combo.Name,
                    combo.LinkedAction.ActionId);
            }
        }

        private static void ValidateCosts(
            ComboConfiguration combo,
            ComboValidationLimits limits,
            ComboValidationResult result)
        {
            if (combo.TotalLoadCost > limits.MaximumLoad)
            {
                result.Add(ComboValidationErrorCode.LoadCostExceeded, combo.Name);
            }

            if (combo.TotalManaCost > limits.MaximumMana)
            {
                result.Add(ComboValidationErrorCode.ManaCostExceeded, combo.Name);
            }

            if (combo.TotalStaminaCost > limits.MaximumStamina)
            {
                result.Add(ComboValidationErrorCode.StaminaCostExceeded, combo.Name);
            }
        }
    }

    public static class ComboLoadoutValidator
    {
        public static ComboValidationResult Validate(
            IReadOnlyList<ComboConfiguration> combos,
            ComboValidationLimits limits)
        {
            if (combos == null)
            {
                throw new ArgumentNullException(nameof(combos));
            }

            if (limits == null)
            {
                throw new ArgumentNullException(nameof(limits));
            }

            var result = new ComboValidationResult();
            if (combos.Count == 0)
            {
                result.Add(ComboValidationErrorCode.EmptyLoadout, comboName: null);
                return result;
            }

            var assignedActionIds = new HashSet<string>(StringComparer.Ordinal);

            for (int comboIndex = 0; comboIndex < combos.Count; comboIndex++)
            {
                ComboConfiguration combo = combos[comboIndex];
                if (combo == null)
                {
                    result.Add(ComboValidationErrorCode.NullCombo, comboName: null);
                    continue;
                }

                ComboValidationResult comboResult = ComboValidator.Validate(combo, limits);
                result.AddRange(comboResult.Issues);
                ValidateUniqueActions(combo, assignedActionIds, result);
            }

            return result;
        }

        private static void ValidateUniqueActions(
            ComboConfiguration combo,
            ISet<string> assignedActionIds,
            ComboValidationResult result)
        {
            IReadOnlyList<ComboActionDefinition> actions = combo.GetAssignedActions();

            for (int actionIndex = 0; actionIndex < actions.Count; actionIndex++)
            {
                ComboActionDefinition action = actions[actionIndex];
                if (!assignedActionIds.Add(action.ActionId))
                {
                    result.Add(
                        ComboValidationErrorCode.ActionAssignedMoreThanOnce,
                        combo.Name,
                        action.ActionId);
                }
            }
        }
    }
}
