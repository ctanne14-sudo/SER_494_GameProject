using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    [DisallowMultipleComponent]
    public sealed class ExplorationBattleDemoLoadoutPreparer : MonoBehaviour
    {
        [Serializable]
        public sealed class ComboAssetSet
        {
            [SerializeField]
            private ComboActionAsset opening;

            [SerializeField]
            private ComboActionAsset main;

            [SerializeField]
            private ComboActionAsset closing;

            [SerializeField]
            private ComboActionAsset linked;

            [SerializeField]
            private LinkedActionPlacement linkedPlacement = LinkedActionPlacement.AfterMain;

            public ComboAssetSet(
                ComboActionAsset opening,
                ComboActionAsset main,
                ComboActionAsset closing,
                ComboActionAsset linked,
                LinkedActionPlacement linkedPlacement)
            {
                this.opening = opening;
                this.main = main;
                this.closing = closing;
                this.linked = linked;
                this.linkedPlacement = linkedPlacement;
            }

            public bool TryCreateConfiguration(
                int zeroBasedIndex,
                out ComboConfiguration configuration,
                out string validationError)
            {
                configuration = null;
                if (!TryCreateAction(opening, "Opening", out ComboActionDefinition openingAction, out validationError)
                    || !TryCreateAction(main, "Main", out ComboActionDefinition mainAction, out validationError)
                    || !TryCreateAction(closing, "Closing", out ComboActionDefinition closingAction, out validationError)
                    || !TryCreateAction(linked, "Linked", out ComboActionDefinition linkedAction, out validationError))
                {
                    validationError = $"Demo Combo {zeroBasedIndex + 1}: {validationError}";
                    return false;
                }

                if (mainAction == null)
                {
                    validationError = $"Demo Combo {zeroBasedIndex + 1} requires a Main Action.";
                    return false;
                }

                configuration = new ComboConfiguration(
                    $"Integration Demo {zeroBasedIndex + 1}",
                    openingAction,
                    mainAction,
                    closingAction,
                    linkedAction,
                    linkedAction == null ? null : (LinkedActionPlacement?)linkedPlacement);
                validationError = null;
                return true;
            }

            private static bool TryCreateAction(
                ComboActionAsset asset,
                string slotName,
                out ComboActionDefinition definition,
                out string validationError)
            {
                if (asset == null)
                {
                    definition = null;
                    validationError = null;
                    return true;
                }

                if (!asset.TryCreateDefinition(out definition, out validationError))
                {
                    validationError = $"{slotName} Action '{asset.name}' is invalid: {validationError}";
                    return false;
                }

                return true;
            }
        }

        private const int ComboCount = 4;
        private const int MaximumTimelineCost = 30;
        private const int MaximumMana = 30;
        private const int MaximumStamina = 30;

        [SerializeField]
        private SoloComboLoadoutSession loadoutSession;

        [SerializeField]
        private WeaponDefinitionAsset equippedWeapon;

        [SerializeField]
        private SoloComboTestScenario testScenario = SoloComboTestScenario.GreatswordTrial;

        [SerializeField]
        private ComboAssetSet[] combos = Array.Empty<ComboAssetSet>();

        private void Awake()
        {
            if (!TryPrepare(out string validationError))
            {
                Debug.LogError(
                    $"Could not prepare the exploration battle demo loadout: {validationError}",
                    this);
            }
        }

        public void Configure(
            SoloComboLoadoutSession session,
            WeaponDefinitionAsset weapon,
            SoloComboTestScenario scenario,
            ComboAssetSet[] comboSets)
        {
            loadoutSession = session;
            equippedWeapon = weapon;
            testScenario = scenario;
            combos = comboSets ?? Array.Empty<ComboAssetSet>();
        }

        public bool TryPrepare(out string validationError)
        {
            if (loadoutSession == null)
            {
                validationError = "A runtime Loadout Session is required.";
                return false;
            }

            if (PlayerStateService.TryGetActive(out PlayerStateService playerStateService))
            {
                return PlayerStateLoadoutSessionBuilder.TryPrepare(
                    playerStateService.State,
                    loadoutSession,
                    testScenario,
                    out validationError);
            }

            if (equippedWeapon == null)
            {
                validationError = "A valid Weapon asset is required.";
                return false;
            }

            if (!equippedWeapon.TryCreateDefinition(
                    out WeaponDefinition weapon,
                    out string weaponError))
            {
                validationError = $"The demo Weapon is invalid: {weaponError}";
                return false;
            }

            if (combos == null || combos.Length != ComboCount)
            {
                validationError = $"The demo requires exactly {ComboCount} Combos.";
                return false;
            }

            var configurations = new List<ComboConfiguration>(ComboCount);
            for (int index = 0; index < combos.Length; index++)
            {
                if (combos[index] == null)
                {
                    validationError = $"Demo Combo {index + 1} is missing.";
                    return false;
                }

                if (!combos[index].TryCreateConfiguration(
                        index,
                        out ComboConfiguration configuration,
                        out validationError))
                {
                    return false;
                }

                configurations.Add(configuration);
            }

            ComboValidationResult result = ComboLoadoutValidator.Validate(
                configurations,
                new ComboValidationLimits(
                    MaximumTimelineCost,
                    MaximumMana,
                    MaximumStamina),
                weapon.WeaponType);
            if (!result.IsValid)
            {
                ComboValidationIssue firstIssue = result.Issues[0];
                validationError =
                    $"{firstIssue.Code} in '{firstIssue.ComboName}'"
                    + (string.IsNullOrWhiteSpace(firstIssue.ActionId)
                        ? "."
                        : $" ({firstIssue.ActionId}).");
                return false;
            }

            loadoutSession.Prepare(configurations, weapon, testScenario);
            validationError = null;
            return true;
        }
    }
}
