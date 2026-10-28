using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.PlayerState;
using SER494.Gameplay.PlayerState.Unity;
using UnityEditor;
using UnityEngine;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

namespace SER494.Gameplay.CombatIntegration.Unity.Tests
{
    /// <summary>
    /// Guards the committed gameplay content, rather than temporary test fixtures.
    /// These tests intentionally fail when an authored asset is invalid, omitted
    /// from its catalog, or referenced by initial Player State without ownership.
    /// </summary>
    [Category("CombatIntegration")]
    [Category("ContentContract")]
    public sealed class AuthoredContentContractTests
    {
        private const string ActionAssetFolder =
            "Assets/GameData/SoloCombo/WeaponActions";
        private const string EncounterAssetFolder =
            "Assets/GameData/Resources/SoloCombo/Encounters";
        private const string FloorOneScenePath =
            "Assets/_Project/Scenes/Floor1_Cell_A.unity";
        private const string PersistentPickupObjectId =
            "floor1.healing-potion-pickup-01";
        private const string PersistentItemPickupScriptGuid =
            "55bd55a713cf4dff91ec67dcc4e51793";
        private const string PersistentWorldObjectScriptGuid =
            "bf66840a4e9985a49a85a9f824ad765b";

        [Test]
        public void ActionCatalog_ContainsEveryAuthoredActionExactlyOnce()
        {
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null,
                $"Missing Resources/{ComboActionCatalogAsset.DefaultResourcesPath}.asset.");

            IReadOnlyList<ComboActionAsset> authoredActions =
                LoadAssets<ComboActionAsset>(ActionAssetFolder);
            Assert.That(authoredActions, Is.Not.Empty,
                $"No Action assets were found under {ActionAssetFolder}.");

            var authoredIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < authoredActions.Count; index++)
            {
                ComboActionAsset action = authoredActions[index];
                string path = AssetDatabase.GetAssetPath(action);
                Assert.That(action.ActionId, Is.Not.Null.And.Not.Empty,
                    $"Action at '{path}' has no Action ID.");
                Assert.That(authoredIds.Add(action.ActionId), Is.True,
                    $"Multiple authored Actions use ID '{action.ActionId}'.");
                Assert.That(
                    action.TryCreateDefinition(out _, out string assetError),
                    Is.True,
                    $"Action at '{path}' is invalid: {assetError}");
                Assert.That(
                    catalog.TryCreateDefinition(
                        action.ActionId,
                        out _,
                        out string catalogError),
                    Is.True,
                    $"Action '{action.ActionId}' is not usable through the default Catalog: "
                    + catalogError);
            }

            SerializedProperty entries = new SerializedObject(catalog)
                .FindProperty("actions");
            Assert.That(entries, Is.Not.Null,
                "The Action Catalog serialized Action list could not be inspected.");
            Assert.That(entries.arraySize, Is.EqualTo(authoredActions.Count),
                "The default Action Catalog must contain every Action asset exactly once.");

            var catalogIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < entries.arraySize; index++)
            {
                var action = entries.GetArrayElementAtIndex(index)
                    .objectReferenceValue as ComboActionAsset;
                Assert.That(action, Is.Not.Null,
                    $"The default Action Catalog contains a null entry at index {index}.");
                Assert.That(catalogIds.Add(action.ActionId), Is.True,
                    $"The default Action Catalog contains duplicate ID '{action.ActionId}'.");
                Assert.That(authoredIds.Contains(action.ActionId), Is.True,
                    $"Catalog Action '{action.ActionId}' is outside {ActionAssetFolder}.");
            }
        }

        [Test]
        public void InitialPlayerState_ActionSetAndLoadoutsResolveThroughDefaultCatalog()
        {
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null,
                $"Missing Resources/{ComboActionCatalogAsset.DefaultResourcesPath}.asset.");

            PersistentPlayerState state = new PlayerStateInitialValues().CreateState();
            Assert.That(state.ActionLibrary.OwnedActionIds, Is.Not.Empty,
                "Initial Player State must own at least one Action.");

            foreach (string actionId in state.ActionLibrary.OwnedActionIds)
            {
                Assert.That(
                    catalog.TryCreateDefinition(
                        actionId,
                        out _,
                        out string validationError),
                    Is.True,
                    $"Initial Action Set references invalid or unregistered Action "
                    + $"'{actionId}': {validationError}");
            }

            Assert.That(state.ComboLoadouts.Loadouts, Is.Not.Empty,
                "Initial Player State must unlock at least one Combo Loadout.");
            foreach (PlayerComboLoadoutState loadout in state.ComboLoadouts.Loadouts)
            {
                AssertLoadoutReferencesOwnedActions(loadout, state, catalog);
                Assert.That(
                    PlayerStateComboConfigurationBuilder.TryBuildLoadout(
                        loadout,
                        state,
                        catalog,
                        out IReadOnlyList<ComboConfiguration> configurations,
                        out string buildError),
                    Is.True,
                    $"Initial Loadout '{loadout.Name}' cannot be built: {buildError}");

                ComboValidationResult result = ComboLoadoutValidator.Validate(
                    configurations,
                    new ComboValidationLimits(
                        PlayerStateLoadoutSessionBuilder.MaximumTimelineCost,
                        state.MaximumMana,
                        state.MaximumCombatStamina));
                Assert.That(result.IsValid, Is.True,
                    $"Initial Loadout '{loadout.Name}' violates battle rules: "
                    + DescribeIssues(result.Issues));
            }

            Assert.That(
                state.ComboLoadouts.Find(state.ComboLoadouts.ActiveLoadoutId),
                Is.Not.Null,
                "Initial Active Loadout ID must reference an unlocked Loadout.");
        }

        [Test]
        public void EncounterAssets_HaveUniqueIdsAndCreateValidDefinitions()
        {
            IReadOnlyList<SoloComboEncounterDefinitionAsset> encounters =
                LoadAssets<SoloComboEncounterDefinitionAsset>(EncounterAssetFolder);
            Assert.That(encounters, Is.Not.Empty,
                $"No Encounter assets were found under {EncounterAssetFolder}.");

            var encounterIds = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < encounters.Count; index++)
            {
                SoloComboEncounterDefinitionAsset encounter = encounters[index];
                string path = AssetDatabase.GetAssetPath(encounter);
                Assert.That(encounter.EncounterId, Is.Not.Null.And.Not.Empty,
                    $"Encounter at '{path}' has no Encounter ID.");
                Assert.That(encounterIds.Add(encounter.EncounterId), Is.True,
                    $"Multiple Encounter assets use ID '{encounter.EncounterId}'.");
                Assert.That(
                    encounter.TryCreateDefinition(out _, out string validationError),
                    Is.True,
                    $"Encounter at '{path}' is invalid: {validationError}");
            }
        }

        [Test]
        public void RegisteredTestScenarios_LoadTheExpectedEncounterAssets()
        {
            foreach (SoloComboTestScenario scenario in
                Enum.GetValues(typeof(SoloComboTestScenario)))
            {
                Assert.That(
                    SoloComboEncounterLibrary.TryLoad(
                        scenario,
                        out SoloComboEncounterDefinitionAsset asset,
                        out string validationError),
                    Is.True,
                    $"Scenario '{scenario}' cannot load its Encounter: {validationError}");
                Assert.That(asset, Is.Not.Null);
                Assert.That(
                    asset.EncounterId,
                    Is.EqualTo(SoloComboTestScenarioInfo.EncounterId(scenario)),
                    $"Scenario '{scenario}' loaded the wrong Encounter asset.");
                Assert.That(
                    asset.TryCreateDefinition(out _, out string definitionError),
                    Is.True,
                    $"Scenario '{scenario}' maps to an invalid Encounter: {definitionError}");
            }
        }

        [Test]
        public void FloorOneScene_ContainsConfiguredPersistentHealingPotionPickup()
        {
            Assert.That(File.Exists(FloorOneScenePath), Is.True,
                $"Required exploration scene '{FloorOneScenePath}' is missing.");

            string sceneText = File.ReadAllText(FloorOneScenePath);
            Assert.That(sceneText,
                Does.Contain("m_Name: Persistent Healing Potion Pickup"),
                "Floor 1 must contain the authored one-time pickup object.");
            Assert.That(sceneText,
                Does.Contain($"guid: {PersistentItemPickupScriptGuid}"),
                "The pickup must use PersistentItemPickupAction.");
            Assert.That(sceneText,
                Does.Contain($"guid: {PersistentWorldObjectScriptGuid}"),
                "The pickup must use PersistentWorldObject.");
            Assert.That(sceneText,
                Does.Contain($"persistentObjectId: {PersistentPickupObjectId}"),
                "The pickup must keep its stable world-object ID.");
            Assert.That(sceneText,
                Does.Contain("terminalStateId: collected"),
                "The pickup terminal state must remain 'collected'.");
            Assert.That(sceneText,
                Does.Contain("deactivateAtTerminalState: 1"),
                "A collected pickup must deactivate when its saved state is restored.");
            Assert.That(sceneText,
                Does.Contain("itemId: healing-potion"),
                "The authored pickup must grant a Healing Potion.");
            Assert.That(sceneText,
                Does.Contain("amount: 1"),
                "The authored pickup must grant exactly one item.");
        }

        private static void AssertLoadoutReferencesOwnedActions(
            PlayerComboLoadoutState loadout,
            PersistentPlayerState state,
            ComboActionCatalogAsset catalog)
        {
            Assert.That(loadout, Is.Not.Null);
            Assert.That(loadout.Combos.Count,
                Is.EqualTo(PlayerComboLoadoutState.ComboCount));

            for (int comboIndex = 0; comboIndex < loadout.Combos.Count; comboIndex++)
            {
                PreparedComboState combo = loadout.Combos[comboIndex];
                foreach (string actionId in combo.GetAssignedActionIds())
                {
                    Assert.That(state.ActionLibrary.Owns(actionId), Is.True,
                        $"Loadout '{loadout.Name}', Combo {comboIndex + 1} references "
                        + $"unowned Action '{actionId}'.");
                    Assert.That(
                        catalog.TryCreateDefinition(
                            actionId,
                            out _,
                            out string validationError),
                        Is.True,
                        $"Loadout '{loadout.Name}', Combo {comboIndex + 1} references "
                        + $"invalid or unregistered Action '{actionId}': {validationError}");
                }
            }
        }

        private static IReadOnlyList<TAsset> LoadAssets<TAsset>(string folder)
            where TAsset : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:{typeof(TAsset).Name}",
                new[] { folder });
            var assets = new List<TAsset>(guids.Length);
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                Assert.That(asset, Is.Not.Null,
                    $"Asset '{path}' could not be loaded as {typeof(TAsset).Name}.");
                assets.Add(asset);
            }

            return assets;
        }

        private static string DescribeIssues(
            IReadOnlyList<ComboValidationIssue> issues)
        {
            if (issues == null || issues.Count == 0)
            {
                return "no issue details were provided";
            }

            var descriptions = new string[issues.Count];
            for (int index = 0; index < issues.Count; index++)
            {
                ComboValidationIssue issue = issues[index];
                descriptions[index] = string.IsNullOrWhiteSpace(issue.ActionId)
                    ? $"{issue.Code} in '{issue.ComboName}'"
                    : $"{issue.Code} in '{issue.ComboName}' ({issue.ActionId})";
            }

            return string.Join("; ", descriptions);
        }
    }
}
