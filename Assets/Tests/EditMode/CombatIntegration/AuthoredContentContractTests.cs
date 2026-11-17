using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Damage;
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
            "Assets/_Project/Scenes/Exploration/Floor1_Cell_A.unity";
        private const string PersistentPickupObjectId =
            "floor1.healing-potion-pickup-01";
        private const string PersistentItemPickupScriptGuid =
            "55bd55a713cf4dff91ec67dcc4e51793";
        private const string PersistentWorldObjectScriptGuid =
            "bf66840a4e9985a49a85a9f824ad765b";
        private const string ExplorationBattleTriggerScriptGuid =
            "ba2e716a7e2f420aad8ef62f8b885e1a";

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
        public void InitialPlayerState_ActionSetResolvesAndLoadoutsStartEmpty()
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
                Assert.That(
                    PlayerStateComboConfigurationBuilder.TryBuildLoadout(
                        loadout,
                        state,
                        catalog,
                        out IReadOnlyList<ComboConfiguration> configurations,
                        out string buildError),
                    Is.True,
                    $"Initial Loadout '{loadout.Name}' cannot be built: {buildError}");
                Assert.That(configurations, Is.Empty,
                    $"Initial Loadout '{loadout.Name}' should be configured by the player.");
                for (int comboIndex = 0; comboIndex < loadout.Combos.Count; comboIndex++)
                {
                    Assert.That(loadout.Combos[comboIndex].GetAssignedActionIds(), Is.Empty,
                        $"Initial Loadout '{loadout.Name}', Combo {comboIndex + 1} is not empty.");
                }
            }

            Assert.That(
                state.ComboLoadouts.Find(state.ComboLoadouts.ActiveLoadoutId),
                Is.Not.Null,
                "Initial Active Loadout ID must reference an unlocked Loadout.");
        }

        [Test]
        public void GreatswordActions_ContainApprovedNewActionsAndOvercommitPenalty()
        {
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null);

            string[] newActionIds =
            {
                "greatsword.opening.vertical-slash",
                "greatsword.opening.leaping-slash",
                "greatsword.opening.double-draw-slash",
                "greatsword.main.rising-strong-strike",
                "greatsword.main.horizontal-strike",
                "greatsword.main.rage-slash",
                "greatsword.closing.overcommit-thrust",
                "greatsword.closing.leaping-sweep",
                "greatsword.closing.horizontal-strike",
                "greatsword.linked.shoulder-tackle-ii",
                "greatsword.linked.horizontal-strike",
                "greatsword.linked.vertical-horizontal-strike"
            };
            for (int index = 0; index < newActionIds.Length; index++)
            {
                Assert.That(
                    catalog.TryCreateDefinition(
                        newActionIds[index],
                        out _,
                        out string validationError),
                    Is.True,
                    $"New Greatsword Action '{newActionIds[index]}' is missing or invalid: "
                    + validationError);
            }

            ComboActionDefinition shoulderTackle =
                CreateAction(catalog, "greatsword.linked.shoulder-tackle");
            Assert.That(shoulderTackle.DisplayName, Is.EqualTo("Shoulder Tackle I [L]"));

            ComboActionDefinition leapingSweep =
                CreateAction(catalog, "greatsword.opening.leaping-sweep");
            Assert.That(leapingSweep.TimelineCost, Is.EqualTo(9));
            Assert.That(leapingSweep.StaminaCost, Is.EqualTo(5));
            AssertDamage(leapingSweep.ResolutionEvents[0], 6, 1.20d, DamageType.Slashing);
            Assert.That(
                leapingSweep.ResolutionEvents[0].DamageTags
                & DamageResolutionTag.Overcommit,
                Is.Not.EqualTo(DamageResolutionTag.None));
            AssertDisruption(
                leapingSweep.ResolutionEvents[0].OnHitDisruption,
                ActionDisruptionType.Flinch,
                2);
            AssertOvercommitPenalty(leapingSweep.ResolutionEvents[0]);
            Assert.That(leapingSweep.ActiveWindows, Has.Count.EqualTo(1));
            Assert.That(leapingSweep.ActiveWindows[0].StartOffset, Is.EqualTo(4));
            Assert.That(leapingSweep.ActiveWindows[0].EndOffset, Is.EqualTo(6));
            Assert.That(leapingSweep.ActiveWindows[0].SuperArmorLevel, Is.EqualTo(3));

            ComboActionDefinition leapingSlash =
                CreateAction(catalog, "greatsword.opening.leaping-slash");
            AssertOvercommitPenalty(leapingSlash.ResolutionEvents[0]);
            ComboActionDefinition overcommitThrust =
                CreateAction(catalog, "greatsword.closing.overcommit-thrust");
            AssertOvercommitPenalty(overcommitThrust.ResolutionEvents[0]);

            ComboActionDefinition rageSlash =
                CreateAction(catalog, "greatsword.main.rage-slash");
            ActionResolutionDefinition rageBuff = rageSlash.ResolutionEvents[0];
            Assert.That(rageBuff.Offset, Is.EqualTo(3));
            Assert.That(rageBuff.StatusEffect, Is.Not.Null);
            Assert.That(rageBuff.StatusEffect.DurationType,
                Is.EqualTo(CombatStatusDurationType.Rounds));
            Assert.That(rageBuff.StatusEffect.Duration, Is.EqualTo(1));
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
        public void ExplorationIntegrationEncounter_HasExpectedTestEnemyRewards()
        {
            SoloComboEncounterDefinitionAsset encounter = AssetDatabase.LoadAssetAtPath<
                SoloComboEncounterDefinitionAsset>(
                $"{EncounterAssetFolder}/ExplorationIntegrationEncounter.asset");
            Assert.That(encounter, Is.Not.Null);
            Assert.That(
                encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition definition,
                    out string validationError),
                Is.True,
                validationError);

            Assert.That(definition.Rewards.Gold, Is.EqualTo(200));
            Assert.That(definition.Rewards.MaterialRollCount, Is.EqualTo(3));
            Assert.That(definition.Rewards.TotalMaterialWeight, Is.EqualTo(100));
            Assert.That(definition.Rewards.MaterialDrops.Count, Is.EqualTo(3));
            Assert.That(definition.Rewards.MaterialDrops[0].ItemId,
                Is.EqualTo(BattleRewardSummary.WhiteCopperMaterialId));
            Assert.That(definition.Rewards.MaterialDrops[0].Weight, Is.EqualTo(30));
            Assert.That(definition.Rewards.MaterialDrops[1].ItemId,
                Is.EqualTo(BattleRewardSummary.BronzeMaterialId));
            Assert.That(definition.Rewards.MaterialDrops[1].Weight, Is.EqualTo(35));
            Assert.That(definition.Rewards.MaterialDrops[2].ItemId,
                Is.EqualTo(BattleRewardSummary.BrassMaterialId));
            Assert.That(definition.Rewards.MaterialDrops[2].Weight, Is.EqualTo(35));
        }

        [Test]
        public void ProcessingShop_ApeBeastRecipesUseEncounterRewardMaterialIds()
        {
            Assert.That(
                ProcessingRecipeCatalog.ApeBeastPeltId,
                Is.EqualTo(BattleRewardSummary.ApeBeastPeltMaterialId));
            Assert.That(
                ProcessingRecipeCatalog.ApeBeastArmguardId,
                Is.EqualTo(BattleRewardSummary.ApeBeastArmguardMaterialId));
            Assert.That(
                ProcessingRecipeCatalog.ApeBeastBlackFurId,
                Is.EqualTo(BattleRewardSummary.ApeBeastBlackFurMaterialId));
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
        public void ReusableEnemyEncounters_MatchApprovedStatsRewardsAndCycles()
        {
            AssertReusableEncounter(
                "DungeonGuardEncounter.asset",
                "encounter.dungeon-guard",
                "Dungeon Guard",
                maximumHealth: 200,
                attack: 50,
                defense: 10,
                rewardGold: 150,
                materialRolls: 3,
                materialIds: new[]
                {
                    BattleRewardSummary.BronzeMaterialId,
                    BattleRewardSummary.BrassMaterialId,
                    BattleRewardSummary.WhiteCopperMaterialId
                },
                materialWeights: new[] { 34, 33, 33 },
                expectedActionIdsByRound: new[]
                {
                    new[]
                    {
                        "enemy.dungeon-guard.action-1",
                        "enemy.dungeon-guard.action-2",
                        "enemy.dungeon-guard.action-3"
                    },
                    new[]
                    {
                        "enemy.dungeon-guard.action-4",
                        "enemy.dungeon-guard.action-5",
                        "enemy.dungeon-guard.action-6"
                    },
                    new[]
                    {
                        "enemy.dungeon-guard.action-5",
                        "enemy.dungeon-guard.action-1",
                        "enemy.dungeon-guard.action-4"
                    }
                });

            AssertReusableEncounter(
                "ApeBeastEncounter.asset",
                "encounter.ape-beast",
                "Ape Beast",
                maximumHealth: 450,
                attack: 60,
                defense: 0,
                rewardGold: 300,
                materialRolls: 5,
                materialIds: new[]
                {
                    BattleRewardSummary.ApeBeastPeltMaterialId,
                    BattleRewardSummary.ApeBeastArmguardMaterialId,
                    BattleRewardSummary.ApeBeastBlackFurMaterialId,
                    BattleRewardSummary.ApeBeastBloodMaterialId
                },
                materialWeights: new[] { 30, 30, 30, 10 },
                expectedActionIdsByRound: new[]
                {
                    new[]
                    {
                        "enemy.ape-beast.action-4",
                        "enemy.ape-beast.action-2",
                        "enemy.ape-beast.action-1"
                    },
                    new[]
                    {
                        "enemy.ape-beast.action-3",
                        "enemy.ape-beast.action-6",
                        "enemy.ape-beast.action-3"
                    },
                    new[]
                    {
                        "enemy.ape-beast.action-3",
                        "enemy.ape-beast.action-5",
                        "enemy.ape-beast.action-4"
                    }
                });

            AssertReusableEncounter(
                "ArmoredGuardEncounter.asset",
                "encounter.armored-guard",
                "Armored Guard",
                maximumHealth: 250,
                attack: 50,
                defense: 30,
                rewardGold: 250,
                materialRolls: 5,
                materialIds: new[]
                {
                    BattleRewardSummary.BronzeMaterialId,
                    BattleRewardSummary.BrassMaterialId,
                    BattleRewardSummary.WhiteCopperMaterialId
                },
                materialWeights: new[] { 34, 33, 33 },
                expectedActionIdsByRound: new[]
                {
                    new[]
                    {
                        "enemy.armored-guard.action-1",
                        "enemy.armored-guard.action-2",
                        "enemy.armored-guard.action-3"
                    },
                    new[]
                    {
                        "enemy.armored-guard.action-4",
                        "enemy.armored-guard.action-5",
                        "enemy.armored-guard.action-6"
                    },
                    new[]
                    {
                        "enemy.armored-guard.action-2",
                        "enemy.armored-guard.action-6",
                        "enemy.armored-guard.action-3"
                    },
                    new[]
                    {
                        "enemy.armored-guard.action-4",
                        "enemy.armored-guard.action-7"
                    }
                });
        }

        [Test]
        public void FloorOneScene_ContainsAllReusableEnemyEncounterTriggers()
        {
            Assert.That(File.Exists(FloorOneScenePath), Is.True,
                $"Required exploration scene '{FloorOneScenePath}' is missing.");

            string sceneText = File.ReadAllText(FloorOneScenePath);
            AssertSceneEncounter(
                sceneText,
                "Dungeon Guard Encounter",
                "floor1.dungeon-guard.01",
                "1a052278eb0049f45b8555e7513ffd96");
            AssertSceneEncounter(
                sceneText,
                "Ape Beast Encounter",
                "floor1.ape-beast.01",
                "353f88149cb05954fa4719ba7229e944");
            AssertSceneEncounter(
                sceneText,
                "Armored Guard Encounter",
                "floor1.armored-guard.01",
                "893fa0362ac143148a461c1f64563fc3");
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

        private static void AssertReusableEncounter(
            string assetName,
            string expectedEncounterId,
            string expectedEnemyName,
            int maximumHealth,
            int attack,
            int defense,
            int rewardGold,
            int materialRolls,
            string[] materialIds,
            int[] materialWeights,
            string[][] expectedActionIdsByRound)
        {
            SoloComboEncounterDefinitionAsset encounter = AssetDatabase.LoadAssetAtPath<
                SoloComboEncounterDefinitionAsset>(
                $"{EncounterAssetFolder}/{assetName}");
            Assert.That(encounter, Is.Not.Null,
                $"Missing reusable Encounter asset '{assetName}'.");
            Assert.That(
                encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition definition,
                    out string validationError),
                Is.True,
                validationError);

            Assert.That(definition.EncounterId, Is.EqualTo(expectedEncounterId));
            Assert.That(definition.EnemyDisplayName, Is.EqualTo(expectedEnemyName));
            Assert.That(definition.EnemyStats.MaxHealth, Is.EqualTo(maximumHealth));
            Assert.That(definition.EnemyStats.Attack, Is.EqualTo(attack));
            Assert.That(definition.EnemyStats.Defense, Is.EqualTo(defense));
            Assert.That(definition.Rewards.Gold, Is.EqualTo(rewardGold));
            Assert.That(definition.Rewards.MaterialRollCount, Is.EqualTo(materialRolls));
            Assert.That(definition.Rewards.TotalMaterialWeight, Is.EqualTo(100));
            Assert.That(definition.Rewards.MaterialDrops.Count,
                Is.EqualTo(materialIds.Length));
            Assert.That(materialWeights.Length, Is.EqualTo(materialIds.Length),
                "A reusable Encounter test has mismatched reward expectations.");
            for (int index = 0; index < materialIds.Length; index++)
            {
                Assert.That(definition.Rewards.MaterialDrops[index].ItemId,
                    Is.EqualTo(materialIds[index]));
                Assert.That(definition.Rewards.MaterialDrops[index].Weight,
                    Is.EqualTo(materialWeights[index]));
            }

            var player = new CombatantState(
                "Player",
                new CombatantStats(100, 0, 0, 50, 50));
            var enemy = new CombatantState(
                definition.EnemyDisplayName,
                definition.EnemyStats);
            for (int roundIndex = 0;
                roundIndex < expectedActionIdsByRound.Length;
                roundIndex++)
            {
                EnemyActionSequence sequence = definition.EnemyBehavior.SelectSequence(
                    player,
                    enemy,
                    previousSequenceId: null,
                    normalizedRoll: 0d);
                string[] expectedActionIds = expectedActionIdsByRound[roundIndex];
                Assert.That(sequence.Actions.Count, Is.EqualTo(expectedActionIds.Length),
                    $"{expectedEnemyName}, Round {roundIndex + 1} has the wrong Action count.");
                for (int actionIndex = 0;
                    actionIndex < expectedActionIds.Length;
                    actionIndex++)
                {
                    Assert.That(sequence.Actions[actionIndex].ActionId,
                        Is.EqualTo(expectedActionIds[actionIndex]),
                        $"{expectedEnemyName}, Round {roundIndex + 1}, "
                        + $"Action {actionIndex + 1} is incorrect.");
                }
            }

            EnemyActionSequence repeatedFirstRound =
                definition.EnemyBehavior.SelectSequence(
                    player,
                    enemy,
                    previousSequenceId: null,
                    normalizedRoll: 0d);
            Assert.That(repeatedFirstRound.Actions[0].ActionId,
                Is.EqualTo(expectedActionIdsByRound[0][0]),
                $"{expectedEnemyName}'s scripted cycle must repeat after its final Round.");
        }

        private static void AssertSceneEncounter(
            string sceneText,
            string objectName,
            string encounterInstanceId,
            string encounterAssetGuid)
        {
            string objectMarker = $"m_Name: {objectName}";
            int objectStart = sceneText.IndexOf(objectMarker, StringComparison.Ordinal);
            Assert.That(objectStart, Is.GreaterThanOrEqualTo(0),
                $"Floor 1 is missing '{objectName}'.");

            int nextGameObject = sceneText.IndexOf(
                "--- !u!1 &",
                objectStart + objectMarker.Length,
                StringComparison.Ordinal);
            string objectBlock = nextGameObject >= 0
                ? sceneText.Substring(objectStart, nextGameObject - objectStart)
                : sceneText.Substring(objectStart);
            Assert.That(objectBlock,
                Does.Contain($"guid: {ExplorationBattleTriggerScriptGuid}"),
                $"'{objectName}' must use ExplorationBattleTrigger.");
            Assert.That(objectBlock,
                Does.Contain($"encounter: {{fileID: 11400000, guid: {encounterAssetGuid}"),
                $"'{objectName}' references the wrong Encounter asset.");
            Assert.That(objectBlock,
                Does.Contain($"encounterInstanceId: {encounterInstanceId}"),
                $"'{objectName}' must keep its stable world-state ID.");
            Assert.That(objectBlock,
                Does.Contain("battleSceneName: CombatSandbox"),
                $"'{objectName}' must enter the formal combat scene.");
        }

        private static ComboActionDefinition CreateAction(
            ComboActionCatalogAsset catalog,
            string actionId)
        {
            Assert.That(
                catalog.TryCreateDefinition(
                    actionId,
                    out ComboActionDefinition definition,
                    out string validationError),
                Is.True,
                validationError);
            return definition;
        }

        private static void AssertDamage(
            ActionResolutionDefinition resolution,
            int offset,
            double multiplier,
            DamageType damageType)
        {
            Assert.That(resolution.Offset, Is.EqualTo(offset));
            Assert.That(resolution.DamageEffect, Is.Not.Null);
            Assert.That(resolution.DamageEffect.Components, Has.Count.EqualTo(1));
            Assert.That(resolution.DamageEffect.Components[0].Type,
                Is.EqualTo(damageType));
            Assert.That(resolution.DamageEffect.Components[0].ActionMultiplier,
                Is.EqualTo(multiplier).Within(0.0001d));
        }

        private static void AssertOvercommitPenalty(ActionResolutionDefinition resolution)
        {
            Assert.That(
                resolution.DamageTags & DamageResolutionTag.Overcommit,
                Is.Not.EqualTo(DamageResolutionTag.None));
            AssertDisruption(
                resolution.OnEvadedDisruption,
                ActionDisruptionType.Stagger,
                2);
        }

        private static void AssertDisruption(
            ActionDisruptionDefinition disruption,
            ActionDisruptionType type,
            int duration)
        {
            Assert.That(disruption, Is.Not.Null);
            Assert.That(disruption.Type, Is.EqualTo(type));
            Assert.That(disruption.Duration, Is.EqualTo(duration));
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
