using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Editor.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SER494.Gameplay.Combat.Editor.CombatIntegration
{
    public static class CombatIntegrationDemoSceneCreator
    {
        private const string FloorScenePath = "Assets/_Project/Scenes/Exploration/Floor1_Cell_A.unity";
        private const string EncounterPath =
            "Assets/GameData/Resources/SoloCombo/Encounters/ExplorationIntegrationEncounter.asset";
        private const string LoadoutSessionPath =
            "Assets/GameData/SoloCombo/SoloComboLoadoutSession.asset";
        private const string GreatswordPath =
            "Assets/GameData/SoloCombo/SampleWeapons/Greatsword.asset";
        private const string ActionFolder = "Assets/GameData/SoloCombo/WeaponActions";
        private const string DemoEnemyName = "Combat Integration Demo Enemy";
        private const string EncounterInstanceId = "floor1.integration-demo.enemy-01";

        [MenuItem("Tools/SER494/Create Floor 1 Combat Integration Demo")]
        public static void CreateOrRepairDemo()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SoloComboLoadoutSession loadoutSession =
                AssetDatabase.LoadAssetAtPath<SoloComboLoadoutSession>(LoadoutSessionPath);
            WeaponDefinitionAsset weapon =
                AssetDatabase.LoadAssetAtPath<WeaponDefinitionAsset>(GreatswordPath);
            if (loadoutSession == null || weapon == null)
            {
                Debug.LogError(
                    "Combat integration demo assets are missing. Repair the Combo Builder "
                    + "prototype scene before creating the Floor 1 demo.");
                return;
            }

            if (!TryLoadGreatswordCombos(
                    out ExplorationBattleDemoLoadoutPreparer.ComboAssetSet[] combos,
                    out string assetError))
            {
                Debug.LogError(assetError);
                return;
            }

            SoloComboEncounterDefinitionAsset encounter = CreateOrUpdateEncounter();
            Scene scene = EditorSceneManager.OpenScene(FloorScenePath, OpenSceneMode.Single);
            GameObject player = FindPlayer(scene);
            if (player == null)
            {
                Debug.LogError(
                    $"Could not find an active GameObject tagged Player in {FloorScenePath}.");
                return;
            }

            GameObject enemy = FindSceneObject(scene, DemoEnemyName);
            bool isNew = enemy == null;
            if (isNew)
            {
                enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = DemoEnemyName;
                Undo.RegisterCreatedObjectUndo(enemy, "Create Combat Integration Demo Enemy");
                SceneManager.MoveGameObjectToScene(enemy, scene);
                enemy.transform.SetPositionAndRotation(
                    player.transform.position + player.transform.forward * 5f + Vector3.up,
                    Quaternion.LookRotation(-player.transform.forward, Vector3.up));
            }
            else
            {
                Undo.RecordObject(enemy, "Repair Combat Integration Demo Enemy");
            }

            Collider triggerCollider = enemy.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                triggerCollider = Undo.AddComponent<CapsuleCollider>(enemy);
            }

            triggerCollider.isTrigger = true;
            EditorUtility.SetDirty(triggerCollider);

            ExplorationBattleDemoLoadoutPreparer preparer =
                GetOrAddComponent<ExplorationBattleDemoLoadoutPreparer>(enemy);
            preparer.Configure(
                loadoutSession,
                weapon,
                SoloComboTestScenario.GreatswordTrial,
                combos);
            EditorUtility.SetDirty(preparer);

            ExplorationBattleTrigger trigger = GetOrAddComponent<ExplorationBattleTrigger>(enemy);
            Undo.RecordObject(trigger, "Configure Combat Integration Demo Trigger");
            trigger.Configure(
                encounter,
                loadoutSession,
                EncounterInstanceId,
                "CombatSandbox");
            EditorUtility.SetDirty(trigger);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FloorScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = enemy;
            EditorGUIUtility.PingObject(enemy);
            Debug.Log(
                $"Created or repaired '{DemoEnemyName}' in {FloorScenePath}. "
                + "Move the selected capsule to a nearby open floor area if needed, then "
                + "enter Play Mode and walk into it.");
        }

        private static SoloComboEncounterDefinitionAsset CreateOrUpdateEncounter()
        {
            SoloComboEncounterDefinitionAsset encounter =
                AssetDatabase.LoadAssetAtPath<SoloComboEncounterDefinitionAsset>(EncounterPath);
            bool isNew = encounter == null;
            if (isNew)
            {
                encounter = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
                encounter.name = "ExplorationIntegrationEncounter";
            }
            else
            {
                Undo.RecordObject(encounter, "Update Exploration Integration Encounter");
            }

            encounter.Configure(
                "integration.floor1.mock-wanderer",
                "Floor 1 Integration Encounter",
                "A weak single-enemy encounter used to verify exploration entry, combat, and victory return.",
                "Training Wanderer",
                newEnemyMaximumHealth: 80,
                newEnemyAttack: 10,
                newEnemyDefense: 20,
                newManaRecoveryPerTurn: 1,
                newStaminaRecoveryPerTurn: 30,
                newStartingItems: Array.Empty<EncounterItemAuthoringData>(),
                newScriptedSequences: new[]
                {
                    new EnemySequenceAuthoringData(
                        "integration.floor1.mock-wanderer.poke",
                        "Measured Poke",
                        new EnemyActionAuthoringData(
                            "integration.floor1.mock-wanderer.poke.action",
                            "Measured Poke",
                            timelineCost: 6,
                            resolutionEffect: ActionResolutionEffect.Damage,
                            damageMultiplier: 0.20f))
                });

            if (!encounter.TryCreateDefinition(out _, out string validationError))
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(encounter);
                }

                throw new InvalidOperationException(
                    $"The integration Encounter is invalid: {validationError}");
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(encounter, EncounterPath);
                Undo.RegisterCreatedObjectUndo(encounter, "Create Exploration Integration Encounter");
            }

            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static bool TryLoadGreatswordCombos(
            out ExplorationBattleDemoLoadoutPreparer.ComboAssetSet[] combos,
            out string validationError)
        {
            var assetsById = new Dictionary<string, ComboActionAsset>(StringComparer.Ordinal);
            string[] assetGuids = AssetDatabase.FindAssets(
                "t:ComboActionAsset",
                new[] { ActionFolder });
            for (int index = 0; index < assetGuids.Length; index++)
            {
                ComboActionAsset asset = AssetDatabase.LoadAssetAtPath<ComboActionAsset>(
                    AssetDatabase.GUIDToAssetPath(assetGuids[index]));
                if (asset != null && !string.IsNullOrWhiteSpace(asset.ActionId))
                {
                    assetsById[asset.ActionId] = asset;
                }
            }

            string[,] ids =
            {
                { "greatsword.opening.quick-draw-slash", "greatsword.main.charged-slash", "greatsword.closing.empowered-sheathe", "greatsword.linked.shoulder-tackle" },
                { "greatsword.opening.leaping-sweep", "greatsword.main.clash-slash", "greatsword.closing.sidestep-sheathe", "greatsword.linked.guarding-charge" },
                { "greatsword.opening.sweeping-slash", "greatsword.main.strong-charged-slash", "greatsword.closing.steadfast-slash", "common.linked.breather-1" },
                { "greatsword.opening.draw-slash", "greatsword.main.vertical-slash", "greatsword.closing.double-sweep", "greatsword.linked.vertical-slash" }
            };
            var placements = new[]
            {
                LinkedActionPlacement.AfterMain,
                LinkedActionPlacement.AfterMain,
                LinkedActionPlacement.AfterClosing,
                LinkedActionPlacement.AfterMain
            };

            combos = new ExplorationBattleDemoLoadoutPreparer.ComboAssetSet[4];
            for (int comboIndex = 0; comboIndex < combos.Length; comboIndex++)
            {
                var slotAssets = new ComboActionAsset[4];
                for (int slotIndex = 0; slotIndex < slotAssets.Length; slotIndex++)
                {
                    string id = ids[comboIndex, slotIndex];
                    if (!assetsById.TryGetValue(id, out slotAssets[slotIndex]))
                    {
                        validationError =
                            $"Combat integration demo requires Action '{id}'. Run the "
                            + "physical/common Action creation tools and repair the Combo Builder scene.";
                        combos = null;
                        return false;
                    }
                }

                combos[comboIndex] = new ExplorationBattleDemoLoadoutPreparer.ComboAssetSet(
                    slotAssets[0],
                    slotAssets[1],
                    slotAssets[2],
                    slotAssets[3],
                    placements[comboIndex]);
            }

            validationError = null;
            return true;
        }

        private static GameObject FindPlayer(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
                for (int index = 0; index < transforms.Length; index++)
                {
                    GameObject candidate = transforms[index].gameObject;
                    if (candidate.activeInHierarchy && candidate.CompareTag("Player"))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (string.Equals(roots[index].name, objectName, StringComparison.Ordinal))
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
        }
    }
}
