using System;
using System.Collections.Generic;
using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using SER494.Gameplay.PlayerState;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Tests
{
    [Category("CombatIntegration")]
    [Category("PlayerState")]
    public sealed class ActionManualRewardRollerTests
    {
        private const string OtherWeaponActionId =
            "sword-and-shield.opening.horizontal-slash";

        [Test]
        public void RollBelowFiftyPercent_SelectsOnlyRemainingOtherWeaponAction()
        {
            ComboActionCatalogAsset catalog = LoadCatalog(
                out IReadOnlyList<ComboActionDefinition> definitions);
            PersistentPlayerState state = CreateStateOwningAllExcept(
                definitions,
                OtherWeaponActionId);

            bool rolled = ActionManualRewardRoller.TryRoll(
                state,
                catalog,
                new Random(1),
                out string actionId,
                out string error);

            Assert.That(rolled, Is.True, error);
            Assert.That(actionId, Is.EqualTo(OtherWeaponActionId));
        }

        [Test]
        public void RollAtOrAboveFiftyPercent_GrantsNoManual()
        {
            ComboActionCatalogAsset catalog = LoadCatalog(out _);
            var state = new PersistentPlayerState(100, 30, 80);

            bool rolled = ActionManualRewardRoller.TryRoll(
                state,
                catalog,
                new Random(0),
                out string actionId,
                out string error);

            Assert.That(rolled, Is.True, error);
            Assert.That(actionId, Is.Null);
        }

        [Test]
        public void HeldManual_IsExcludedAndEmptyPoolDoesNotRollDuplicate()
        {
            ComboActionCatalogAsset catalog = LoadCatalog(
                out IReadOnlyList<ComboActionDefinition> definitions);
            PersistentPlayerState state = CreateStateOwningAllExcept(
                definitions,
                OtherWeaponActionId);
            string manualId = ActionManualLearningService.CreateManualItemId(OtherWeaponActionId);
            state.Inventory.Add(manualId, 1);

            bool rolled = ActionManualRewardRoller.TryRoll(
                state,
                catalog,
                new Random(1),
                out string actionId,
                out string error);

            Assert.That(rolled, Is.True, error);
            Assert.That(actionId, Is.Null);
            Assert.That(state.Inventory.GetCount(manualId), Is.EqualTo(1));
        }

        [Test]
        public void AllLearnedActions_ProduceNoManual()
        {
            ComboActionCatalogAsset catalog = LoadCatalog(
                out IReadOnlyList<ComboActionDefinition> definitions);
            var state = new PersistentPlayerState(100, 30, 80);
            for (int index = 0; index < definitions.Count; index++)
            {
                state.ActionLibrary.Learn(definitions[index].ActionId);
            }

            bool rolled = ActionManualRewardRoller.TryRoll(
                state,
                catalog,
                new Random(1),
                out string actionId,
                out string error);

            Assert.That(rolled, Is.True, error);
            Assert.That(actionId, Is.Null);
        }

        private static ComboActionCatalogAsset LoadCatalog(
            out IReadOnlyList<ComboActionDefinition> definitions)
        {
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null);
            Assert.That(
                catalog.TryCreateAllDefinitions(out definitions, out string error),
                Is.True,
                error);
            Assert.That(definitions, Is.Not.Empty);
            return catalog;
        }

        private static PersistentPlayerState CreateStateOwningAllExcept(
            IReadOnlyList<ComboActionDefinition> definitions,
            string excludedActionId)
        {
            var state = new PersistentPlayerState(100, 30, 80);
            bool foundExcludedAction = false;
            for (int index = 0; index < definitions.Count; index++)
            {
                string actionId = definitions[index].ActionId;
                if (actionId == excludedActionId)
                {
                    foundExcludedAction = true;
                    continue;
                }

                state.ActionLibrary.Learn(actionId);
            }

            Assert.That(foundExcludedAction, Is.True,
                $"The Action Catalog is missing '{excludedActionId}'.");
            return state;
        }
    }
}
