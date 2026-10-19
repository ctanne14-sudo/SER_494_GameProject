using System.Linq;
using NUnit.Framework;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.Weapons;

namespace SER494.Gameplay.Combat.SoloCombo.Tests
{
    public sealed class ComboValidatorTests
    {
        private static readonly ComboValidationLimits GenerousLimits =
            new ComboValidationLimits(maximumTimelineCost: 20, maximumMana: 20, maximumStamina: 20);

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
                    timelineCost: 11,
                    manaCost: 13,
                    staminaCost: 17,
                    cooldown: 0),
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);
            var limits = new ComboValidationLimits(10, 12, 16);

            ComboValidationResult result = ComboValidator.Validate(combo, limits);

            ComboValidationErrorCode[] codes = result.Issues.Select(issue => issue.Code).ToArray();
            Assert.That(codes, Contains.Item(ComboValidationErrorCode.TimelineCostExceeded));
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

        [Test]
        public void Validate_DamageAndGuardAtSameTime_IsInvalid()
        {
            var damage = new ComboActionDefinition(
                "opening-damage",
                "Opening Damage",
                ComboActionCategory.Opening,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0.20d,
                resolutionOffset: 1);
            var guard = new ComboActionDefinition(
                "main-guard",
                "Main Guard",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                damageMultiplier: 0d,
                resolutionOffset: 0,
                resolutionEffect: ActionResolutionEffect.Guard);
            var combo = new ComboConfiguration(
                "invalid-overlap",
                damage,
                guard,
                closingAction: null,
                linkedAction: null,
                linkedActionPlacement: null);

            ComboValidationResult result = ComboValidator.Validate(combo, GenerousLimits);

            Assert.That(
                result.Issues.Select(issue => issue.Code),
                Contains.Item(ComboValidationErrorCode.MutuallyExclusiveResolutionEffectsOverlap));
        }

        [Test]
        public void Validate_ActionCompatibleWithEquippedWeapon_IsValid()
        {
            var main = new ComboActionDefinition(
                "bow-main",
                "Bow Main",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                compatibleWeaponTypes: CompatibleWeaponTypes.Bow);

            ComboValidationResult result = ComboValidator.Validate(
                MainOnlyCombo("combo1", main),
                GenerousLimits,
                WeaponType.Bow);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_ActionIncompatibleWithEquippedWeapon_IsInvalid()
        {
            var main = new ComboActionDefinition(
                "bow-main",
                "Bow Main",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 1,
                cooldown: 0,
                compatibleWeaponTypes: CompatibleWeaponTypes.Bow);

            ComboValidationResult result = ComboValidator.Validate(
                MainOnlyCombo("combo1", main),
                GenerousLimits,
                WeaponType.Greatsword);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Issues.Select(issue => issue.Code),
                Contains.Item(ComboValidationErrorCode.ActionIncompatibleWithEquippedWeapon));
        }

        [Test]
        public void Validate_LinkedAfterStatusSetupAction_IsValid()
        {
            ComboActionDefinition closing = StatusSetupAction(
                "jump-closing",
                ComboActionCategory.Closing,
                "airborne-setup");
            ComboActionDefinition linked = new ComboActionDefinition(
                "airborne-linked",
                "Airborne Linked",
                ComboActionCategory.Linked,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                previousActionStatusRequirement: new PreviousActionStatusRequirementDefinition(
                    "airborne-setup",
                    "Airborne Setup"));
            var combo = new ComboConfiguration(
                "airborne-combo",
                openingAction: null,
                mainAction: Action("main", ComboActionCategory.Main),
                closingAction: closing,
                linkedAction: linked,
                linkedActionPlacement: LinkedActionPlacement.AfterClosing);

            ComboValidationResult result = ComboValidator.Validate(combo, GenerousLimits);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Validate_RequiredStatusIsNotProvidedByImmediatelyPreviousAction_IsInvalid()
        {
            ComboActionDefinition main = new ComboActionDefinition(
                "airborne-main",
                "Airborne Main",
                ComboActionCategory.Main,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                previousActionStatusRequirement: new PreviousActionStatusRequirementDefinition(
                    "airborne-setup",
                    "Airborne Setup"));

            ComboValidationResult result = ComboValidator.Validate(
                MainOnlyCombo("invalid-airborne-combo", main),
                GenerousLimits);

            Assert.That(result.IsValid, Is.False);
            Assert.That(
                result.Issues.Select(issue => issue.Code),
                Contains.Item(
                    ComboValidationErrorCode.PreviousActionDoesNotProvideRequiredStatus));
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
                timelineCost: 1,
                manaCost: 1,
                staminaCost: 1,
                cooldown: 1);
        }

        private static ComboActionDefinition StatusSetupAction(
            string id,
            ComboActionCategory category,
            string statusId)
        {
            var status = new CombatStatusDefinition(
                statusId,
                "Airborne Setup",
                CombatStatusDurationType.Rounds,
                duration: 1,
                CombatStatusModifierDirection.DamageDealt,
                DamageModifier.All(0d));
            return new ComboActionDefinition(
                id,
                id,
                category,
                timelineCost: 1,
                manaCost: 0,
                staminaCost: 0,
                cooldown: 0,
                resolutionEvents: new[]
                {
                    new ActionResolutionDefinition(
                        offset: 1,
                        effect: ActionResolutionEffect.ApplyBuff,
                        statusEffect: status,
                        statusTarget: ActionStatusTarget.Self)
                });
        }
    }
}
