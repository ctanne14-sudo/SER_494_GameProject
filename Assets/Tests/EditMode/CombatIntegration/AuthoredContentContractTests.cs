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
        public void SwordAndShieldActions_ContainApprovedNewActionsAndUpdatedBehavior()
        {
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null);

            string[] newActionIds =
            {
                "sword-and-shield.opening.double-slash",
                "sword-and-shield.opening.dashing-shield-bash",
                "sword-and-shield.opening.sliding-assault",
                "sword-and-shield.main.precision-slash",
                "sword-and-shield.main.sword-dance-1",
                "sword-and-shield.main.sword-dance-2",
                "sword-and-shield.closing.fourfold-turning-slash",
                "sword-and-shield.closing.shield-bash-jump",
                "sword-and-shield.closing.turning-backhand-slash",
                "sword-and-shield.linked.shield-bash",
                "sword-and-shield.linked.sword-shield-combo",
                "sword-and-shield.linked.precision-counter"
            };
            for (int index = 0; index < newActionIds.Length; index++)
            {
                Assert.That(
                    catalog.TryCreateDefinition(
                        newActionIds[index],
                        out _,
                        out string validationError),
                    Is.True,
                    $"New Sword and Shield Action '{newActionIds[index]}' is missing or invalid: "
                    + validationError);
            }

            ComboActionDefinition dashingShieldBash =
                CreateAction(catalog, "sword-and-shield.opening.dashing-shield-bash");
            Assert.That(dashingShieldBash.TimelineCost, Is.EqualTo(7));
            Assert.That(dashingShieldBash.StaminaCost, Is.EqualTo(2));
            AssertDamage(dashingShieldBash.ResolutionEvents[0], 6, 0.80d, DamageType.Blunt);
            Assert.That(dashingShieldBash.ActiveWindows, Has.Count.EqualTo(1));
            Assert.That(dashingShieldBash.ActiveWindows[0].StartOffset, Is.EqualTo(1));
            Assert.That(dashingShieldBash.ActiveWindows[0].EndOffset, Is.EqualTo(5));
            Assert.That(dashingShieldBash.ActiveWindows[0].DamageReduction,
                Is.EqualTo(0.20d).Within(0.0001d));

            ComboActionDefinition slidingAssault =
                CreateAction(catalog, "sword-and-shield.opening.sliding-assault");
            AssertResolutionWindow(
                slidingAssault.ResolutionEvents[0],
                ActionResolutionEffect.Evade,
                2,
                4);
            AssertDamage(slidingAssault.ResolutionEvents[1], 6, 0.40d, DamageType.Slashing);
            AssertDamage(slidingAssault.ResolutionEvents[2], 7, 0.30d, DamageType.Blunt);

            ComboActionDefinition precisionSlash =
                CreateAction(catalog, "sword-and-shield.main.precision-slash");
            AssertDamage(precisionSlash.ResolutionEvents[0], 3, 0.60d, DamageType.Slashing);
            AssertResolutionWindow(
                precisionSlash.ResolutionEvents[0],
                ActionResolutionEffect.Damage,
                3,
                5);

            ComboActionDefinition shieldBashJump =
                CreateAction(catalog, "sword-and-shield.closing.shield-bash-jump");
            Assert.That(shieldBashJump.ResolutionEvents[1].StatusEffect, Is.Not.Null);
            Assert.That(shieldBashJump.ResolutionEvents[1].StatusEffect.StatusId,
                Is.EqualTo("sequence.airborne-setup"));

            ComboActionDefinition aerialShieldBash =
                CreateAction(catalog, "sword-and-shield.closing.aerial-shield-bash");
            Assert.That(aerialShieldBash.PreviousActionStatusRequirement, Is.Not.Null);
            Assert.That(aerialShieldBash.PreviousActionStatusRequirement.StatusId,
                Is.EqualTo("sequence.airborne-setup"));
            AssertDisruption(
                aerialShieldBash.ResolutionEvents[1].OnHitDisruption,
                ActionDisruptionType.Flinch,
                1);

            ComboActionDefinition closingBackhand =
                CreateAction(catalog, "sword-and-shield.closing.turning-backhand-slash");
            AssertResolutionWindow(
                closingBackhand.ResolutionEvents[0],
                ActionResolutionEffect.Evade,
                2,
                4);
            AssertDamage(closingBackhand.ResolutionEvents[1], 5, 0.50d, DamageType.Slashing);
            AssertDamage(closingBackhand.ResolutionEvents[2], 6, 0.30d, DamageType.Slashing);

            ComboActionDefinition linkedBackhand =
                CreateAction(catalog, "sword-and-shield.linked.turning-backhand-slash");
            Assert.That(linkedBackhand.TimelineCost, Is.EqualTo(6));
            Assert.That(linkedBackhand.StaminaCost, Is.EqualTo(4));
            AssertDamage(linkedBackhand.ResolutionEvents[1], 4, 0.50d, DamageType.Slashing);

            ComboActionDefinition precisionCounter =
                CreateAction(catalog, "sword-and-shield.linked.precision-counter");
            AssertResolutionWindow(
                precisionCounter.ResolutionEvents[0],
                ActionResolutionEffect.Guard,
                1,
                3);
            AssertDamage(precisionCounter.ResolutionEvents[1], 4, 0.40d, DamageType.Slashing);
        }

        [Test]
        public void GlaiveAndShieldActions_ContainApprovedNewActionsAndUpdatedBehavior()
        {
            ComboActionCatalogAsset catalog = ComboActionCatalogAsset.LoadDefault();
            Assert.That(catalog, Is.Not.Null);

            string[] newActionIds =
            {
                "glaive-and-shield.opening.diagonal-slash",
                "glaive-and-shield.opening.heavy-strike-jump",
                "glaive-and-shield.opening.evasive-shield-bash",
                "glaive-and-shield.main.aerial-assault-thrust",
                "glaive-and-shield.main.fivefold-coil",
                "glaive-and-shield.main.aerial-whirling-slash",
                "glaive-and-shield.closing.evasive-slash",
                "glaive-and-shield.closing.aerial-assault-thrust",
                "glaive-and-shield.closing.hook-strike-retreat",
                "glaive-and-shield.linked.heavy-strike-jump",
                "glaive-and-shield.linked.evasive-slash",
                "glaive-and-shield.linked.hook-strike"
            };
            for (int index = 0; index < newActionIds.Length; index++)
            {
                Assert.That(
                    catalog.TryCreateDefinition(
                        newActionIds[index],
                        out _,
                        out string validationError),
                    Is.True,
                    $"New Glaive and Shield Action '{newActionIds[index]}' is missing or invalid: "
                    + validationError);
            }

            ComboActionDefinition diagonalSlash =
                CreateAction(catalog, "glaive-and-shield.opening.diagonal-slash");
            Assert.That(diagonalSlash.TimelineCost, Is.EqualTo(7));
            Assert.That(diagonalSlash.StaminaCost, Is.EqualTo(3));
            AssertDamage(diagonalSlash.ResolutionEvents[0], 4, 0.50d, DamageType.Slashing);
            AssertDamage(diagonalSlash.ResolutionEvents[1], 6, 0.50d, DamageType.Slashing);

            ComboActionDefinition openingJump =
                CreateAction(catalog, "glaive-and-shield.opening.heavy-strike-jump");
            Assert.That(openingJump.TimelineCost, Is.EqualTo(7));
            Assert.That(openingJump.StaminaCost, Is.EqualTo(5));
            AssertDamage(openingJump.ResolutionEvents[0], 3, 0.40d, DamageType.Slashing);
            AssertDamage(openingJump.ResolutionEvents[1], 5, 0.60d, DamageType.Blunt);
            Assert.That(openingJump.ResolutionEvents[2].StatusEffect, Is.Not.Null);
            Assert.That(openingJump.ResolutionEvents[2].Offset, Is.EqualTo(6));
            Assert.That(openingJump.ResolutionEvents[2].StatusEffect.StatusId,
                Is.EqualTo("sequence.airborne-setup"));

            ComboActionDefinition evasiveShieldBash =
                CreateAction(catalog, "glaive-and-shield.opening.evasive-shield-bash");
            AssertResolutionWindow(
                evasiveShieldBash.ResolutionEvents[0],
                ActionResolutionEffect.Evade,
                3,
                5);
            AssertDamage(
                evasiveShieldBash.ResolutionEvents[1],
                6,
                0.60d,
                DamageType.Blunt);
            AssertDisruption(
                evasiveShieldBash.ResolutionEvents[1].OnHitDisruption,
                ActionDisruptionType.Flinch,
                1);

            ComboActionDefinition assaultThrust =
                CreateAction(catalog, "glaive-and-shield.main.aerial-assault-thrust");
            Assert.That(assaultThrust.PreviousActionStatusRequirement, Is.Not.Null);
            Assert.That(assaultThrust.PreviousActionStatusRequirement.StatusId,
                Is.EqualTo("sequence.airborne-setup"));
            AssertDamage(assaultThrust.ResolutionEvents[0], 3, 0.80d, DamageType.Piercing);
            AssertDamage(assaultThrust.ResolutionEvents[1], 6, 0.30d, DamageType.Slashing);
            AssertDamage(assaultThrust.ResolutionEvents[2], 8, 1.00d, DamageType.Slashing);

            ComboActionDefinition fivefoldCoil =
                CreateAction(catalog, "glaive-and-shield.main.fivefold-coil");
            Assert.That(fivefoldCoil.TimelineCost, Is.EqualTo(9));
            Assert.That(fivefoldCoil.StaminaCost, Is.EqualTo(11));
            Assert.That(fivefoldCoil.ResolutionEvents[0].Effect,
                Is.EqualTo(ActionResolutionEffect.Guard));
            Assert.That(fivefoldCoil.ResolutionEvents[0].Offset, Is.EqualTo(2));
            AssertResolutionWindow(
                fivefoldCoil.ResolutionEvents[3],
                ActionResolutionEffect.Guard,
                5,
                7);
            AssertDamage(fivefoldCoil.ResolutionEvents[4], 8, 0.80d, DamageType.Piercing);

            ComboActionDefinition aerialWhirlingSlash =
                CreateAction(catalog, "glaive-and-shield.main.aerial-whirling-slash");
            Assert.That(aerialWhirlingSlash.PreviousActionStatusRequirement, Is.Not.Null);
            Assert.That(aerialWhirlingSlash.ResolutionEvents[0].Effect,
                Is.EqualTo(ActionResolutionEffect.Evade));
            Assert.That(aerialWhirlingSlash.ResolutionEvents[2].Effect,
                Is.EqualTo(ActionResolutionEffect.Evade));
            Assert.That(aerialWhirlingSlash.ResolutionEvents[4].Effect,
                Is.EqualTo(ActionResolutionEffect.Evade));
            Assert.That(aerialWhirlingSlash.ResolutionEvents[6].StatusEffect, Is.Not.Null);
            Assert.That(aerialWhirlingSlash.ResolutionEvents[6].StatusEffect.StatusId,
                Is.EqualTo("sequence.airborne-setup"));

            ComboActionDefinition closingEvasiveSlash =
                CreateAction(catalog, "glaive-and-shield.closing.evasive-slash");
            Assert.That(closingEvasiveSlash.ResolutionEvents[0].Effect,
                Is.EqualTo(ActionResolutionEffect.Evade));
            AssertDamage(
                closingEvasiveSlash.ResolutionEvents[1],
                3,
                0.20d,
                DamageType.Slashing);
            AssertResolutionWindow(
                closingEvasiveSlash.ResolutionEvents[2],
                ActionResolutionEffect.Evade,
                5,
                7);

            ComboActionDefinition retreat =
                CreateAction(catalog, "glaive-and-shield.closing.hook-strike-retreat");
            AssertResolutionWindow(
                retreat.ResolutionEvents[0],
                ActionResolutionEffect.Guard,
                1,
                3);
            AssertDamage(retreat.ResolutionEvents[1], 4, 0.20d, DamageType.Slashing);
            AssertDamage(retreat.ResolutionEvents[2], 5, 0.10d, DamageType.Piercing);
            AssertDamage(retreat.ResolutionEvents[3], 7, 0.50d, DamageType.Slashing);
            Assert.That(retreat.ResolutionEvents[4].Effect,
                Is.EqualTo(ActionResolutionEffect.Evade));

            ComboActionDefinition linkedJump =
                CreateAction(catalog, "glaive-and-shield.linked.heavy-strike-jump");
            Assert.That(linkedJump.StaminaCost, Is.EqualTo(6));
            Assert.That(linkedJump.ResolutionEvents[2].StatusEffect, Is.Not.Null);

            ComboActionDefinition linkedEvasiveSlash =
                CreateAction(catalog, "glaive-and-shield.linked.evasive-slash");
            Assert.That(linkedEvasiveSlash.StaminaCost, Is.EqualTo(6));
            AssertResolutionWindow(
                linkedEvasiveSlash.ResolutionEvents[2],
                ActionResolutionEffect.Evade,
                5,
                7);

            ComboActionDefinition hookStrike =
                CreateAction(catalog, "glaive-and-shield.linked.hook-strike");
            Assert.That(hookStrike.TimelineCost, Is.EqualTo(8));
            Assert.That(hookStrike.StaminaCost, Is.EqualTo(5));
            AssertResolutionWindow(
                hookStrike.ResolutionEvents[0],
                ActionResolutionEffect.Guard,
                1,
                3);
            AssertDamage(hookStrike.ResolutionEvents[3], 7, 0.50d, DamageType.Slashing);
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
        public void ProcessingShop_ExpandedMaterialsUseEncounterRewardMaterialIds()
        {
            Assert.That(ProcessingRecipeCatalog.SlimeChunkId,
                Is.EqualTo(BattleRewardSummary.SlimeChunkMaterialId));
            Assert.That(ProcessingRecipeCatalog.ThornId,
                Is.EqualTo(BattleRewardSummary.ThornMaterialId));
            Assert.That(ProcessingRecipeCatalog.CrystalId,
                Is.EqualTo(BattleRewardSummary.CrystalMaterialId));
            Assert.That(ProcessingRecipeCatalog.PureGoldId,
                Is.EqualTo(BattleRewardSummary.PureGoldMaterialId));
            Assert.That(ProcessingRecipeCatalog.MithrilId,
                Is.EqualTo(BattleRewardSummary.MithrilMaterialId));
            Assert.That(ProcessingRecipeCatalog.YellowSilkId,
                Is.EqualTo(BattleRewardSummary.YellowSilkMaterialId));
            Assert.That(ProcessingRecipeCatalog.RedSilkId,
                Is.EqualTo(BattleRewardSummary.RedSilkMaterialId));
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
        public void ExpandedEnemyEncounters_MatchApprovedStatsRewardsAndCycles()
        {
            AssertExpandedEncounter(
                "ThornSlimeEncounter.asset",
                "encounter.thorn-slime",
                "Thorn Slime",
                450,
                40,
                15,
                150,
                3,
                new[]
                {
                    BattleRewardSummary.ThornMaterialId,
                    BattleRewardSummary.SlimeChunkMaterialId
                },
                new[] { 50, 50 },
                null,
                null,
                new[]
                {
                    new[] { 1, 5, 3 },
                    new[] { 2, 6, 4 },
                    new[] { 1, 4, 2 },
                    new[] { 5, 7, 3 }
                });
            AssertExpandedEncounter(
                "OreMonsterEncounter.asset",
                "encounter.ore-monster",
                "Ore Monster",
                700,
                50,
                40,
                400,
                5,
                new[]
                {
                    BattleRewardSummary.CrystalMaterialId,
                    BattleRewardSummary.PureGoldMaterialId,
                    BattleRewardSummary.MithrilMaterialId,
                    BattleRewardSummary.WhiteCopperMaterialId,
                    BattleRewardSummary.BronzeMaterialId,
                    BattleRewardSummary.BrassMaterialId
                },
                new[] { 17, 16, 17, 18, 16, 16 },
                null,
                null,
                new[]
                {
                    new[] { 1, 4, 5 },
                    new[] { 2, 3, 1 },
                    new[] { 2, 7, 1 },
                    new[] { 3, 5, 6 }
                });
            AssertExpandedEncounter(
                "EliteOreMonsterEncounter.asset",
                "encounter.elite-ore-monster",
                "Elite Ore Monster",
                800,
                50,
                50,
                500,
                5,
                new[]
                {
                    BattleRewardSummary.CrystalMaterialId,
                    BattleRewardSummary.PureGoldMaterialId,
                    BattleRewardSummary.MithrilMaterialId
                },
                new[] { 33, 34, 33 },
                null,
                null,
                new[]
                {
                    new[] { 1, 4, 5 },
                    new[] { 2, 3, 1 },
                    new[] { 2, 7, 1 },
                    new[] { 3, 5, 6 }
                });

            int[] thiefGoldAmounts = { 50, 100, 150, 200, 1000 };
            int[] thiefGoldWeights = { 35, 31, 21, 10, 3 };
            string[] thiefMaterials =
            {
                BattleRewardSummary.YellowSilkMaterialId,
                BattleRewardSummary.RedSilkMaterialId,
                BattleRewardSummary.CrystalMaterialId,
                BattleRewardSummary.PureGoldMaterialId,
                BattleRewardSummary.MithrilMaterialId
            };
            int[] thiefMaterialWeights = { 41, 41, 6, 6, 6 };
            AssertExpandedEncounter(
                "DungeonThiefIEncounter.asset",
                "encounter.dungeon-thief-1",
                "Dungeon Thief I",
                250,
                30,
                0,
                0,
                3,
                thiefMaterials,
                thiefMaterialWeights,
                thiefGoldAmounts,
                thiefGoldWeights,
                new[]
                {
                    new[] { 1, 4, 5 },
                    new[] { 3, 6, 5 },
                    new[] { 1, 2, 3 }
                });
            AssertExpandedEncounter(
                "DungeonThiefIIEncounter.asset",
                "encounter.dungeon-thief-2",
                "Dungeon Thief II",
                250,
                35,
                10,
                0,
                3,
                thiefMaterials,
                thiefMaterialWeights,
                thiefGoldAmounts,
                thiefGoldWeights,
                new[]
                {
                    new[] { 6, 1, 2 },
                    new[] { 2, 5, 4 },
                    new[] { 3, 4, 1 }
                });
        }

        [Test]
        public void ExpandedEnemyActions_PreserveApprovedTimingAndSpecialRules()
        {
            AssertActionOffsets("ThornSlimeEncounter.asset", 4,
                new[] { new[] { 4, 7 }, new[] { 6, 7, 8 }, new[] { 3, 4, 8 },
                    new[] { 7 }, new[] { 4, 5 }, new[] { 1, 2, 3, 9 },
                    new[] { 3, 4, 5, 6, 7, 8 } });
            AssertActionOffsets("OreMonsterEncounter.asset", 4,
                new[] { new[] { 3, 5, 8 }, new[] { 2, 4, 6, 7 }, new[] { 3, 6 },
                    new[] { 3, 6 }, new[] { 1, 5, 9 }, new[] { 2, 4, 7, 9 },
                    new[] { 4, 8 } });
            AssertActionOffsets("EliteOreMonsterEncounter.asset", 4,
                new[] { new[] { 3, 5, 8 }, new[] { 2, 4, 6, 7 }, new[] { 3, 6 },
                    new[] { 3, 6 }, new[] { 1, 5, 9 }, new[] { 2, 4, 7, 9 },
                    new[] { 4, 8 } });
            AssertActionOffsets("DungeonThiefIEncounter.asset", 3,
                new[] { new[] { 5 }, new[] { 3, 8 }, new[] { 2, 6, 10 },
                    new[] { 1, 5, 7 }, new[] { 5, 9 }, new[] { 2, 5, 8 } });
            AssertActionOffsets("DungeonThiefIIEncounter.asset", 3,
                new[] { new[] { 5 }, new[] { 6, 8 }, new[] { 2, 5, 8 },
                    new[] { 1, 6, 8 }, new[] { 1, 4, 5, 6, 9 },
                    new[] { 2, 5, 8 } });

            EnemyActionDefinition thornDefense = FindExpandedAction(
                "ThornSlimeEncounter.asset", 4, "enemy.thorn-slime.action-4");
            Assert.That(thornDefense.ActiveWindows, Has.Count.EqualTo(1));
            Assert.That(thornDefense.ActiveWindows[0].Effect,
                Is.EqualTo(ActiveWindowEffect.DefensiveState));
            Assert.That(thornDefense.ActiveWindows[0].DamageReduction,
                Is.EqualTo(0.50d).Within(0.0001d));
            Assert.That(thornDefense.ActiveWindows[0].SuperArmorLevel, Is.Zero);

            EnemyActionDefinition thornBuff = FindExpandedAction(
                "ThornSlimeEncounter.asset", 4, "enemy.thorn-slime.action-5");
            Assert.That(thornBuff.ResolutionEvents[0].StatusEffect.Duration, Is.EqualTo(30));
            Assert.That(thornBuff.ResolutionEvents[1].StatusEffect.Duration, Is.EqualTo(30));
            Assert.That(thornBuff.ResolutionEvents[0].StatusEffect.ModifierDirection,
                Is.EqualTo(CombatStatusModifierDirection.DamageTaken));
            Assert.That(thornBuff.ResolutionEvents[1].StatusEffect.ModifierDirection,
                Is.EqualTo(CombatStatusModifierDirection.DamageDealt));

            AssertOvercommitSelfStagger(
                FindExpandedAction(
                    "OreMonsterEncounter.asset", 4, "enemy.ore-monster.action-7"));
            AssertOvercommitSelfStagger(
                FindExpandedAction(
                    "EliteOreMonsterEncounter.asset", 4,
                    "enemy.elite-ore-monster.action-7"));

            EnemyActionDefinition thiefDefense = FindExpandedAction(
                "DungeonThiefIIEncounter.asset", 3, "enemy.dungeon-thief-2.action-2");
            Assert.That(thiefDefense.ActiveWindows, Has.Count.EqualTo(1));
            Assert.That(thiefDefense.ActiveWindows[0].DamageReduction,
                Is.EqualTo(0.10d).Within(0.0001d));
            Assert.That(thiefDefense.ActiveWindows[0].SuperArmorLevel, Is.Zero);
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

        private static void AssertExpandedEncounter(
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
            int[] goldAmounts,
            int[] goldWeights,
            int[][] expectedActionNumbersByRound)
        {
            SoloComboEncounterDefinition definition = LoadExpandedEncounter(assetName);
            Assert.That(definition.EncounterId, Is.EqualTo(expectedEncounterId));
            Assert.That(definition.EnemyDisplayName, Is.EqualTo(expectedEnemyName));
            Assert.That(definition.EnemyStats.MaxHealth, Is.EqualTo(maximumHealth));
            Assert.That(definition.EnemyStats.Attack, Is.EqualTo(attack));
            Assert.That(definition.EnemyStats.Defense, Is.EqualTo(defense));
            Assert.That(definition.Rewards.Gold, Is.EqualTo(rewardGold));
            Assert.That(definition.Rewards.MaterialRollCount, Is.EqualTo(materialRolls));
            Assert.That(definition.Rewards.TotalMaterialWeight, Is.EqualTo(100));
            Assert.That(definition.Rewards.MaterialDrops.Count, Is.EqualTo(materialIds.Length));
            for (int index = 0; index < materialIds.Length; index++)
            {
                Assert.That(definition.Rewards.MaterialDrops[index].ItemId,
                    Is.EqualTo(materialIds[index]));
                Assert.That(definition.Rewards.MaterialDrops[index].Weight,
                    Is.EqualTo(materialWeights[index]));
            }

            int expectedGoldDropCount = goldAmounts?.Length ?? 0;
            Assert.That(definition.Rewards.GoldDrops.Count, Is.EqualTo(expectedGoldDropCount));
            Assert.That(definition.Rewards.TotalGoldWeight,
                Is.EqualTo(expectedGoldDropCount == 0 ? 0 : 100));
            for (int index = 0; index < expectedGoldDropCount; index++)
            {
                Assert.That(definition.Rewards.GoldDrops[index].Amount,
                    Is.EqualTo(goldAmounts[index]));
                Assert.That(definition.Rewards.GoldDrops[index].Weight,
                    Is.EqualTo(goldWeights[index]));
            }

            AssertBehaviorCycle(definition, expectedActionNumbersByRound);
        }

        private static void AssertBehaviorCycle(
            SoloComboEncounterDefinition definition,
            int[][] expectedActionNumbersByRound)
        {
            var player = new CombatantState("Player", new CombatantStats(100, 0, 0, 50, 50));
            var enemy = new CombatantState(definition.EnemyDisplayName, definition.EnemyStats);
            for (int roundIndex = 0;
                roundIndex <= expectedActionNumbersByRound.Length;
                roundIndex++)
            {
                EnemyActionSequence sequence = definition.EnemyBehavior.SelectSequence(
                    player, enemy, null, 0d);
                int expectedRound = roundIndex % expectedActionNumbersByRound.Length;
                int[] expectedActions = expectedActionNumbersByRound[expectedRound];
                Assert.That(sequence.Actions.Count, Is.EqualTo(expectedActions.Length));
                for (int actionIndex = 0; actionIndex < expectedActions.Length; actionIndex++)
                {
                    Assert.That(sequence.Actions[actionIndex].ActionId,
                        Does.EndWith($"action-{expectedActions[actionIndex]}"));
                }
            }
        }

        private static void AssertActionOffsets(
            string assetName,
            int roundCount,
            int[][] expectedOffsetsByAction)
        {
            Dictionary<string, EnemyActionDefinition> actions =
                LoadExpandedActions(assetName, roundCount);
            Assert.That(actions.Count, Is.EqualTo(expectedOffsetsByAction.Length));
            for (int actionIndex = 0; actionIndex < expectedOffsetsByAction.Length; actionIndex++)
            {
                string suffix = $"action-{actionIndex + 1}";
                EnemyActionDefinition action = null;
                foreach (KeyValuePair<string, EnemyActionDefinition> pair in actions)
                {
                    if (pair.Key.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        action = pair.Value;
                        break;
                    }
                }

                Assert.That(action, Is.Not.Null,
                    $"{assetName} is missing {suffix}.");
                Assert.That(action.TimelineCost, Is.EqualTo(10));
                Assert.That(action.ResolutionEvents.Count,
                    Is.EqualTo(expectedOffsetsByAction[actionIndex].Length));
                for (int eventIndex = 0;
                    eventIndex < expectedOffsetsByAction[actionIndex].Length;
                    eventIndex++)
                {
                    Assert.That(action.ResolutionEvents[eventIndex].Offset,
                        Is.EqualTo(expectedOffsetsByAction[actionIndex][eventIndex]));
                }
            }
        }

        private static EnemyActionDefinition FindExpandedAction(
            string assetName,
            int roundCount,
            string actionId)
        {
            Dictionary<string, EnemyActionDefinition> actions =
                LoadExpandedActions(assetName, roundCount);
            Assert.That(actions.TryGetValue(actionId, out EnemyActionDefinition action), Is.True,
                $"{assetName} is missing Action '{actionId}'.");
            return action;
        }

        private static Dictionary<string, EnemyActionDefinition> LoadExpandedActions(
            string assetName,
            int roundCount)
        {
            SoloComboEncounterDefinition definition = LoadExpandedEncounter(assetName);
            var actions = new Dictionary<string, EnemyActionDefinition>(StringComparer.Ordinal);
            var player = new CombatantState("Player", new CombatantStats(100, 0, 0, 50, 50));
            var enemy = new CombatantState(definition.EnemyDisplayName, definition.EnemyStats);
            for (int roundIndex = 0; roundIndex < roundCount; roundIndex++)
            {
                EnemyActionSequence sequence = definition.EnemyBehavior.SelectSequence(
                    player, enemy, null, 0d);
                for (int actionIndex = 0; actionIndex < sequence.Actions.Count; actionIndex++)
                {
                    EnemyActionDefinition action = sequence.Actions[actionIndex];
                    actions[action.ActionId] = action;
                }
            }

            return actions;
        }

        private static SoloComboEncounterDefinition LoadExpandedEncounter(string assetName)
        {
            SoloComboEncounterDefinitionAsset encounter = AssetDatabase.LoadAssetAtPath<
                SoloComboEncounterDefinitionAsset>($"{EncounterAssetFolder}/{assetName}");
            Assert.That(encounter, Is.Not.Null,
                $"Missing expanded Encounter asset '{assetName}'.");
            Assert.That(
                encounter.TryCreateDefinition(
                    out SoloComboEncounterDefinition definition,
                    out string validationError),
                Is.True,
                validationError);
            return definition;
        }

        private static void AssertOvercommitSelfStagger(EnemyActionDefinition action)
        {
            ActionResolutionDefinition resolution = action.ResolutionEvents[1];
            Assert.That(resolution.DamageTags & DamageResolutionTag.Overcommit,
                Is.EqualTo(DamageResolutionTag.Overcommit));
            Assert.That(resolution.OnEvadedDisruption, Is.Not.Null);
            Assert.That(resolution.OnEvadedDisruption.Type,
                Is.EqualTo(ActionDisruptionType.Stagger));
            Assert.That(resolution.OnEvadedDisruption.Duration, Is.EqualTo(2));
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

        private static void AssertResolutionWindow(
            ActionResolutionDefinition resolution,
            ActionResolutionEffect effect,
            int start,
            int end)
        {
            Assert.That(resolution.Effect, Is.EqualTo(effect));
            Assert.That(resolution.HasResolutionWindow, Is.True);
            Assert.That(resolution.ResolutionWindowStart, Is.EqualTo(start));
            Assert.That(resolution.ResolutionWindowEnd, Is.EqualTo(end));
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
