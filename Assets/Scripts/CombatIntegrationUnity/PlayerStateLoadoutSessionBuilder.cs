using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.Equipment;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.Combat.Weapons;
using SER494.Gameplay.PlayerState;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    public static class PlayerStateLoadoutSessionBuilder
    {
        private const int MaximumTimelineCost = 30;

        public static bool TryPrepare(
            PersistentPlayerState playerState,
            SoloComboLoadoutSession loadoutSession,
            SoloComboTestScenario testScenario,
            out string validationError)
        {
            return TryPrepare(
                playerState,
                loadoutSession,
                ComboActionCatalogAsset.LoadDefault(),
                PlayerEquipmentCatalogAsset.LoadDefault(),
                testScenario,
                out validationError);
        }

        public static bool TryPrepare(
            PersistentPlayerState playerState,
            SoloComboLoadoutSession loadoutSession,
            ComboActionCatalogAsset actionCatalog,
            PlayerEquipmentCatalogAsset equipmentCatalog,
            SoloComboTestScenario testScenario,
            out string validationError)
        {
            if (playerState == null)
            {
                validationError = "Persistent Player State is unavailable.";
                return false;
            }

            if (loadoutSession == null)
            {
                validationError = "A runtime Loadout Session is required.";
                return false;
            }

            if (actionCatalog == null)
            {
                validationError =
                    $"Combo Action Catalog is missing from Resources/{ComboActionCatalogAsset.DefaultResourcesPath}.asset.";
                return false;
            }

            if (equipmentCatalog == null)
            {
                validationError =
                    $"Player Equipment Catalog is missing from Resources/{PlayerEquipmentCatalogAsset.DefaultResourcesPath}.asset.";
                return false;
            }

            if (!equipmentCatalog.TryGetWeapon(
                    playerState.Equipment.EquippedWeaponId,
                    out WeaponDefinition weapon,
                    out validationError))
            {
                return false;
            }

            PlayerComboLoadoutState activeLoadout = playerState.ComboLoadouts.ActiveLoadout;
            if (activeLoadout == null)
            {
                validationError = "The player has no active Combo Loadout.";
                return false;
            }

            var configurations = new List<ComboConfiguration>(
                activeLoadout.Combos.Count);
            for (int index = 0; index < activeLoadout.Combos.Count; index++)
            {
                if (!TryCreateConfiguration(
                        activeLoadout.Combos[index],
                        playerState,
                        actionCatalog,
                        out ComboConfiguration configuration,
                        out validationError))
                {
                    validationError =
                        $"Loadout '{activeLoadout.Name}', Combo {index + 1}: {validationError}";
                    return false;
                }

                configurations.Add(configuration);
            }

            // Weapon compatibility is intentionally omitted here. It is a warning
            // for Combo Configuration, not a condition for entering battle.
            ComboValidationResult result = ComboLoadoutValidator.Validate(
                configurations,
                new ComboValidationLimits(
                    MaximumTimelineCost,
                    playerState.MaximumMana,
                    playerState.MaximumCombatStamina));
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

        private static bool TryCreateConfiguration(
            PreparedComboState prepared,
            PersistentPlayerState playerState,
            ComboActionCatalogAsset actionCatalog,
            out ComboConfiguration configuration,
            out string validationError)
        {
            configuration = null;
            if (prepared == null)
            {
                validationError = "Prepared Combo data is missing.";
                return false;
            }

            if (!TryCreateOwnedAction(
                    prepared.OpeningActionId,
                    playerState,
                    actionCatalog,
                    out ComboActionDefinition opening,
                    out validationError)
                || !TryCreateOwnedAction(
                    prepared.MainActionId,
                    playerState,
                    actionCatalog,
                    out ComboActionDefinition main,
                    out validationError)
                || !TryCreateOwnedAction(
                    prepared.ClosingActionId,
                    playerState,
                    actionCatalog,
                    out ComboActionDefinition closing,
                    out validationError)
                || !TryCreateOwnedAction(
                    prepared.LinkedActionId,
                    playerState,
                    actionCatalog,
                    out ComboActionDefinition linked,
                    out validationError))
            {
                return false;
            }

            configuration = new ComboConfiguration(
                prepared.Name,
                opening,
                main,
                closing,
                linked,
                ConvertPlacement(prepared.LinkedPlacement));
            validationError = null;
            return true;
        }

        private static bool TryCreateOwnedAction(
            string actionId,
            PersistentPlayerState playerState,
            ComboActionCatalogAsset actionCatalog,
            out ComboActionDefinition definition,
            out string validationError)
        {
            definition = null;
            if (actionId == null)
            {
                validationError = null;
                return true;
            }

            if (!playerState.ActionLibrary.Owns(actionId))
            {
                validationError = $"Action '{actionId}' is not in the player's Action Set.";
                return false;
            }

            return actionCatalog.TryCreateDefinition(
                actionId,
                out definition,
                out validationError);
        }

        private static LinkedActionPlacement? ConvertPlacement(
            PreparedLinkedActionPlacement? placement)
        {
            if (!placement.HasValue)
            {
                return null;
            }

            switch (placement.Value)
            {
                case PreparedLinkedActionPlacement.AfterOpening:
                    return LinkedActionPlacement.AfterOpening;
                case PreparedLinkedActionPlacement.AfterMain:
                    return LinkedActionPlacement.AfterMain;
                case PreparedLinkedActionPlacement.AfterClosing:
                    return LinkedActionPlacement.AfterClosing;
                default:
                    throw new ArgumentOutOfRangeException(nameof(placement));
            }
        }
    }
}
