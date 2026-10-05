using System.Linq;
using NUnit.Framework;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class ComboValidatorTests
    {
        private static readonly ComboValidationLimits GenerousLimits =
            new ComboValidationLimits(maximumLoad: 20, maximumMana: 20, maximumStamina: 20);

        [Test]
        public void Validate_MainOnlyCombo_IsValid()
        {
            ComboConfiguration combo = new ComboConfiguration(
                "combo1",
                openingAction: null,
                mainAction: Action("main", ComboActionCategory.Main),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);

            ComboValidationResult result = ComboValidator.Validate(combo, GenerousLimits);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_MissingMainAction_IsInvalid()
        {
            ComboConfiguration combo = ComboConfiguration.CreateEmpty(1);

            ComboValidationResult result = ComboValidator.Validate(combo, GenerousLimits);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Issues.Select(issue => issue.Code),
                Contains.Item(ComboValidationErrorCode.MissingMainAction));
        }

        [Test]
        public void Validate_LinkedAfterMissingClosingAction_IsInvalid()
        {
            ComboConfiguration combo = new ComboConfiguration(
                "combo1",
                openingAction: null,
                mainAction: Action("main", ComboActionCategory.Main),
                closingAction: null,
                linkedAction: Action("linked", ComboActionCategory.Linked),
                linkedActionPlacement: LinkedActionPlacement.AfterClosing);

            ComboValidationResult result = ComboValidator.Validate(combo, GenerousLimits);

            Assert.That(
                result.Issues.Select(issue => issue.Code),
                Contains.Item(ComboValidationErrorCode.LinkedActionPlacementHasNoAction));
        }

        [Test]
        public void Validate_CostOverCharacterMaximum_IsInvalid()
        {
            ComboConfiguration combo = new ComboConfiguration(
                "combo1",
                openingAction: null,
                mainAction: new ComboActionDefinition(
                    "main",
                    "Main",
                    ComboActionCategory.Main,
                    loadCost: 11,
                    manaCost: 13,
                    staminaCost: 17,
                    cooldown: 0),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var limits = new ComboValidationLimits(10, 12, 16);

            ComboValidationResult result = ComboValidator.Validate(combo, limits);

            ComboValidationErrorCode[] codes = result.Issues.Select(issue => issue.Code).ToArray();
            Assert.That(codes, Contains.Item(ComboValidationErrorCode.LoadCostExceeded));
            Assert.That(codes, Contains.Item(ComboValidationErrorCode.ManaCostExceeded));
            Assert.That(codes, Contains.Item(ComboValidationErrorCode.StaminaCostExceeded));
        }

        [Test]
        public void Validate_SameActionInTwoCombos_IsInvalid()
        {
            ComboActionDefinition sharedMain = Action("shared-main", ComboActionCategory.Main);
            ComboConfiguration first = MainOnlyCombo("combo1", sharedMain);
            ComboConfiguration second = MainOnlyCombo("combo2", sharedMain);

            ComboValidationResult result = ComboLoadoutValidator.Validate(
                new[] { first, second },
                GenerousLimits);

            Assert.That(
                result.Issues.Select(issue => issue.Code),
                Contains.Item(ComboValidationErrorCode.ActionAssignedMoreThanOnce));
        }

        private static ComboConfiguration MainOnlyCombo(
            string comboName,
            ComboActionDefinition mainAction)
        {
            return new ComboConfiguration(
                comboName,
                openingAction: null,
                mainAction,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
        }

        private static ComboActionDefinition Action(string id, ComboActionCategory category)
        {
            return new ComboActionDefinition(
                id,
                id,
                category,
                loadCost: 1,
                manaCost: 1,
                staminaCost: 1,
                cooldown: 1);
        }
    }
}
