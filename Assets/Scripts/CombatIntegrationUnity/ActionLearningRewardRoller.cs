using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.PlayerState;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Unity
{
    public static class ActionLearningRewardRoller
    {
        public const double LearnChance = 0.5d;

        public static bool TryRoll(
            PersistentPlayerState playerState,
            ComboActionCatalogAsset catalog,
            Random random,
            out string actionId,
            out string validationError)
        {
            actionId = null;
            if (playerState == null)
            {
                validationError = "Player State is required to roll a learned Action reward.";
                return false;
            }

            if (catalog == null)
            {
                validationError =
                    $"Action Catalog is missing from Resources/{ComboActionCatalogAsset.DefaultResourcesPath}.asset.";
                return false;
            }

            if (random == null)
            {
                validationError = "A reward random source is required.";
                return false;
            }

            if (!catalog.TryCreateAllDefinitions(
                    out IReadOnlyList<ComboActionDefinition> definitions,
                    out validationError))
            {
                return false;
            }

            var eligibleActionIds = new List<string>(definitions.Count);
            for (int index = 0; index < definitions.Count; index++)
            {
                string candidateId = definitions[index].ActionId;
                string manualItemId = ActionManualLearningService.CreateManualItemId(candidateId);
                if (!playerState.ActionLibrary.Owns(candidateId)
                    && playerState.Inventory.GetCount(manualItemId) == 0)
                {
                    eligibleActionIds.Add(candidateId);
                }
            }

            if (eligibleActionIds.Count == 0 || random.NextDouble() >= LearnChance)
            {
                validationError = null;
                return true;
            }

            actionId = eligibleActionIds[random.Next(eligibleActionIds.Count)];
            validationError = null;
            return true;
        }
    }
}
