using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.Damage;
using SER494.Gameplay.Combat.Editor.SoloCombo;
using SER494.Gameplay.Combat.SoloCombo;
using SER494.Gameplay.CombatIntegration.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SER494.Gameplay.Combat.Editor.CombatIntegration
{
    public static class ReusableEnemyEncounterAssetCreator
    {
        private const string FloorScenePath =
            "Assets/_Project/Scenes/Exploration/Floor1_Cell_A.unity";
        private const string EncounterFolder =
            "Assets/GameData/Resources/SoloCombo/Encounters";
        private const string LoadoutSessionPath =
            "Assets/GameData/SoloCombo/SoloComboLoadoutSession.asset";
        private const string GreatswordPath =
            "Assets/GameData/SoloCombo/SampleWeapons/Greatsword.asset";
        private const string ActionFolder = "Assets/GameData/SoloCombo/WeaponActions";

        private const string LegacyDemoEnemyName = "Combat Integration Demo Enemy";
        private const string DungeonGuardObjectName = "Dungeon Guard Encounter";
        private const string ApeBeastObjectName = "Ape Beast Encounter";
        private const string ArmoredGuardObjectName = "Armored Guard Encounter";

        private const string DungeonGuardEncounterPath =
            EncounterFolder + "/DungeonGuardEncounter.asset";
        private const string ApeBeastEncounterPath =
            EncounterFolder + "/ApeBeastEncounter.asset";
        private const string ArmoredGuardEncounterPath =
            EncounterFolder + "/ArmoredGuardEncounter.asset";

        [MenuItem("Tools/SER494/Create or Update Reusable Enemy Encounters")]
        public static void CreateOrUpdate()
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
                throw new InvalidOperationException(
                    "The reusable Encounter preview requires the default Loadout Session "
                    + "and Greatsword assets.");
            }

            if (!TryLoadGreatswordCombos(
                    out ExplorationBattleDemoLoadoutPreparer.ComboAssetSet[] combos,
                    out string loadoutError))
            {
                throw new InvalidOperationException(loadoutError);
            }

            SoloComboEncounterDefinitionAsset dungeonGuard = CreateDungeonGuardEncounter();
            SoloComboEncounterDefinitionAsset apeBeast = CreateApeBeastEncounter();
            SoloComboEncounterDefinitionAsset armoredGuard = CreateArmoredGuardEncounter();

            Scene scene = EditorSceneManager.OpenScene(FloorScenePath, OpenSceneMode.Single);
            GameObject player = FindPlayer(scene);
            if (player == null)
            {
                throw new InvalidOperationException(
                    $"Could not find an active GameObject tagged Player in {FloorScenePath}.");
            }

            GameObject legacyEnemy = FindSceneObject(scene, LegacyDemoEnemyName);
            GameObject dungeonGuardObject = FindSceneObject(scene, DungeonGuardObjectName);
            if (dungeonGuardObject == null && legacyEnemy != null)
            {
                Undo.RecordObject(legacyEnemy, "Convert Demo Enemy To Dungeon Guard");
                legacyEnemy.name = DungeonGuardObjectName;
                dungeonGuardObject = legacyEnemy;
            }

            Vector3 origin = dungeonGuardObject != null
                ? dungeonGuardObject.transform.position
                : player.transform.position + player.transform.forward * 5f + Vector3.up;
            Vector3 right = player.transform.right;
            Quaternion facing = Quaternion.LookRotation(-player.transform.forward, Vector3.up);

            dungeonGuardObject = CreateOrConfigureSceneEnemy(
                scene,
                DungeonGuardObjectName,
                dungeonGuard,
                loadoutSession,
                weapon,
                combos,
                "floor1.dungeon-guard.01",
                origin - right * 3f,
                facing);
            CreateOrConfigureSceneEnemy(
                scene,
                ApeBeastObjectName,
                apeBeast,
                loadoutSession,
                weapon,
                combos,
                "floor1.ape-beast.01",
                origin,
                facing);
            CreateOrConfigureSceneEnemy(
                scene,
                ArmoredGuardObjectName,
                armoredGuard,
                loadoutSession,
                weapon,
                combos,
                "floor1.armored-guard.01",
                origin + right * 3f,
                facing);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, FloorScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = dungeonGuardObject;
            EditorGUIUtility.PingObject(dungeonGuardObject);
            Debug.Log(
                "Created or updated Dungeon Guard, Ape Beast, and Armored Guard "
                + $"Encounters and their Floor 1 preview objects in {FloorScenePath}.");
        }

        private static SoloComboEncounterDefinitionAsset CreateDungeonGuardEncounter()
        {
            EnemyActionAuthoringData action1 = Action(
                "enemy.dungeon-guard.action-1",
                "Action 1",
                10,
                Evade(5),
                Damage(7, 0.60f, DamageType.Slashing));
            EnemyActionAuthoringData action2 = Action(
                "enemy.dungeon-guard.action-2",
                "Action 2",
                10,
                Damage(3, 0.50f, DamageType.Slashing),
                Damage(8, 0.50f, DamageType.Slashing));
            EnemyActionAuthoringData action3 = Action(
                "enemy.dungeon-guard.action-3",
                "Action 3",
                10,
                Damage(4, 0.60f, DamageType.Slashing),
                Evade(9));
            EnemyActionAuthoringData action4 = Action(
                "enemy.dungeon-guard.action-4",
                "Action 4",
                10,
                Guard(1),
                Evade(5),
                Damage(8, 0.20f, DamageType.Piercing));
            EnemyActionAuthoringData action5 = Action(
                "enemy.dungeon-guard.action-5",
                "Action 5",
                10,
                Damage(2, 0.30f, DamageType.Piercing),
                Damage(6, 0.50f, DamageType.Slashing),
                Damage(8, 0.20f, DamageType.Slashing));
            EnemyActionAuthoringData action6 = Action(
                "enemy.dungeon-guard.action-6",
                "Action 6",
                10,
                Evade(2),
                Damage(5, 0.60f, DamageType.Slashing));

            return CreateOrUpdateEncounter(
                DungeonGuardEncounterPath,
                "encounter.dungeon-guard",
                "Dungeon Guard Encounter",
                "A predictable three-round Dungeon Guard combat cycle.",
                "Dungeon Guard",
                maximumHealth: 200,
                attack: 50,
                defense: 10,
                rewardGold: 150,
                materialRolls: 3,
                materialDrops: CopperDrops(34, 33, 33),
                sequences: new[]
                {
                    Sequence("enemy.dungeon-guard.round-1", "Round 1", action1, action2, action3),
                    Sequence("enemy.dungeon-guard.round-2", "Round 2", action4, action5, action6),
                    Sequence("enemy.dungeon-guard.round-3", "Round 3", action5, action1, action4)
                });
        }

        private static SoloComboEncounterDefinitionAsset CreateApeBeastEncounter()
        {
            EnemyActionAuthoringData action1 = Action(
                "enemy.ape-beast.action-1",
                "Action 1",
                10,
                Damage(6, 1.00f, DamageType.Blunt));
            EnemyActionAuthoringData action2 = Action(
                "enemy.ape-beast.action-2",
                "Action 2",
                10,
                Damage(
                    8,
                    1.00f,
                    DamageType.Blunt,
                    DamageResolutionTag.Clash,
                    clash: new DamageClashDefinition(0.30d, 5)));
            EnemyActionAuthoringData action3 = Action(
                "enemy.ape-beast.action-3",
                "Action 3",
                10,
                Damage(5, 0.60f, DamageType.Blunt));
            EnemyActionAuthoringData action4 = Action(
                "enemy.ape-beast.action-4",
                "Action 4",
                10,
                Damage(2, 0.30f, DamageType.Blunt),
                Damage(4, 0.30f, DamageType.Blunt));
            EnemyActionAuthoringData action5 = Action(
                "enemy.ape-beast.action-5",
                "Action 5",
                10,
                Damage(
                    5,
                    0.30f,
                    DamageType.Blunt,
                    DamageResolutionTag.Overcommit,
                    onEvadedDisruption: new ActionDisruptionDefinition(
                        ActionDisruptionType.Stagger,
                        5)),
                Damage(8, 0.60f, DamageType.Blunt));
            EnemyActionAuthoringData action6 = Action(
                "enemy.ape-beast.action-6",
                "Action 6",
                10,
                Damage(
                    6,
                    0.50f,
                    DamageType.Blunt,
                    DamageResolutionTag.Unguardable));

            return CreateOrUpdateEncounter(
                ApeBeastEncounterPath,
                "encounter.ape-beast",
                "Ape Beast Encounter",
                "A heavy-hitting three-round Ape Beast combat cycle.",
                "Ape Beast",
                maximumHealth: 450,
                attack: 60,
                defense: 0,
                rewardGold: 300,
                materialRolls: 5,
                materialDrops: new[]
                {
                    new EncounterRewardDropAuthoringData(
                        BattleRewardSummary.ApeBeastPeltMaterialId,
                        30),
                    new EncounterRewardDropAuthoringData(
                        BattleRewardSummary.ApeBeastArmguardMaterialId,
                        30),
                    new EncounterRewardDropAuthoringData(
                        BattleRewardSummary.ApeBeastBlackFurMaterialId,
                        30),
                    new EncounterRewardDropAuthoringData(
                        BattleRewardSummary.ApeBeastBloodMaterialId,
                        10)
                },
                sequences: new[]
                {
                    Sequence("enemy.ape-beast.round-1", "Round 1", action4, action2, action1),
                    Sequence("enemy.ape-beast.round-2", "Round 2", action3, action6, action3),
                    Sequence("enemy.ape-beast.round-3", "Round 3", action3, action5, action4)
                });
        }

        private static SoloComboEncounterDefinitionAsset CreateArmoredGuardEncounter()
        {
            EnemyActionAuthoringData action1 = Action(
                "enemy.armored-guard.action-1",
                "Action 1",
                10,
                Guard(5),
                Damage(7, 0.60f, DamageType.Slashing));
            EnemyActionAuthoringData action2 = Action(
                "enemy.armored-guard.action-2",
                "Action 2",
                10,
                Damage(3, 0.50f, DamageType.Slashing),
                Damage(8, 0.50f, DamageType.Slashing));
            EnemyActionAuthoringData action3 = Action(
                "enemy.armored-guard.action-3",
                "Action 3",
                10,
                Damage(4, 0.60f, DamageType.Slashing),
                Guard(9));
            var defenseBuff = new CombatStatusDefinition(
                "status.armored-guard.damage-reduction",
                "Guarded Armor",
                CombatStatusDurationType.TimeSteps,
                20,
                CombatStatusModifierDirection.DamageTaken,
                DamageModifier.All(-0.20d),
                CombatStatusStackingPolicy.IndependentInstances);
            EnemyActionAuthoringData action4 = Action(
                "enemy.armored-guard.action-4",
                "Action 4",
                10,
                new EnemyResolutionEventAuthoringData(
                    3,
                    ActionResolutionEffect.ApplyBuff,
                    statusEffect: defenseBuff,
                    statusTarget: ActionStatusTarget.Self),
                Damage(6, 0.30f, DamageType.Piercing));
            EnemyActionAuthoringData action5 = Action(
                "enemy.armored-guard.action-5",
                "Action 5",
                10,
                Damage(2, 0.30f, DamageType.Piercing),
                Damage(6, 0.50f, DamageType.Slashing),
                Damage(8, 0.20f, DamageType.Slashing));
            EnemyActionAuthoringData action6 = Action(
                "enemy.armored-guard.action-6",
                "Action 6",
                10,
                Evade(2),
                Damage(5, 0.60f, DamageType.Slashing));
            EnemyActionAuthoringData action7 = new EnemyActionAuthoringData(
                "enemy.armored-guard.action-7",
                "Action 7",
                20,
                new[]
                {
                    Damage(3, 0.50f, DamageType.Slashing),
                    Damage(6, 1.00f, DamageType.Slashing),
                    Damage(9, 0.80f, DamageType.Slashing)
                },
                new[]
                {
                    new EnemyActiveWindowAuthoringData(
                        "enemy.armored-guard.action-7.super-armor",
                        "Super Armor III",
                        1,
                        20,
                        ActiveWindowEffect.SuperArmor,
                        superArmorLevel: 3,
                        disruptionPolicy: ActiveWindowDisruptionPolicy.BreakOnDisruption)
                });

            return CreateOrUpdateEncounter(
                ArmoredGuardEncounterPath,
                "encounter.armored-guard",
                "Armored Guard Encounter",
                "A defensive four-round Armored Guard combat cycle.",
                "Armored Guard",
                maximumHealth: 250,
                attack: 50,
                defense: 30,
                rewardGold: 250,
                materialRolls: 5,
                materialDrops: CopperDrops(34, 33, 33),
                sequences: new[]
                {
                    Sequence("enemy.armored-guard.round-1", "Round 1", action1, action2, action3),
                    Sequence("enemy.armored-guard.round-2", "Round 2", action4, action5, action6),
                    Sequence("enemy.armored-guard.round-3", "Round 3", action2, action6, action3),
                    Sequence("enemy.armored-guard.round-4", "Round 4", action4, action7)
                });
        }

        private static SoloComboEncounterDefinitionAsset CreateOrUpdateEncounter(
            string assetPath,
            string encounterId,
            string displayName,
            string description,
            string enemyName,
            int maximumHealth,
            int attack,
            int defense,
            int rewardGold,
            int materialRolls,
            EncounterRewardDropAuthoringData[] materialDrops,
            EnemySequenceAuthoringData[] sequences)
        {
            SoloComboEncounterDefinitionAsset encounter =
                AssetDatabase.LoadAssetAtPath<SoloComboEncounterDefinitionAsset>(assetPath);
            bool isNew = encounter == null;
            if (isNew)
            {
                encounter = ScriptableObject.CreateInstance<SoloComboEncounterDefinitionAsset>();
                encounter.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
            else
            {
                Undo.RecordObject(encounter, $"Update {displayName}");
            }

            encounter.Configure(
                encounterId,
                displayName,
                description,
                enemyName,
                maximumHealth,
                attack,
                defense,
                newManaRecoveryPerTurn: 1,
                newStaminaRecoveryPerTurn: 10,
                newStartingItems: Array.Empty<EncounterItemAuthoringData>(),
                newScriptedSequences: sequences,
                newRewardGold: rewardGold,
                newRewardMaterialRollCount: materialRolls,
                newRewardMaterialDrops: materialDrops);

            if (!encounter.TryCreateDefinition(out _, out string validationError))
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(encounter);
                }

                throw new InvalidOperationException(
                    $"Encounter '{displayName}' is invalid: {validationError}");
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(encounter, assetPath);
                Undo.RegisterCreatedObjectUndo(encounter, $"Create {displayName}");
            }

            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static GameObject CreateOrConfigureSceneEnemy(
            Scene scene,
            string objectName,
            SoloComboEncounterDefinitionAsset encounter,
            SoloComboLoadoutSession loadoutSession,
            WeaponDefinitionAsset weapon,
            ExplorationBattleDemoLoadoutPreparer.ComboAssetSet[] combos,
            string encounterInstanceId,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject enemy = FindSceneObject(scene, objectName);
            if (enemy == null)
            {
                enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemy.name = objectName;
                Undo.RegisterCreatedObjectUndo(enemy, $"Create {objectName}");
                SceneManager.MoveGameObjectToScene(enemy, scene);
                enemy.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                Undo.RecordObject(enemy, $"Update {objectName}");
            }

            Undo.RecordObject(enemy.transform, $"Position {objectName}");
            enemy.transform.SetPositionAndRotation(position, rotation);
            EditorUtility.SetDirty(enemy.transform);

            Collider collider = enemy.GetComponent<Collider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<CapsuleCollider>(enemy);
            }

            collider.isTrigger = true;
            EditorUtility.SetDirty(collider);

            ExplorationBattleDemoLoadoutPreparer preparer =
                GetOrAddComponent<ExplorationBattleDemoLoadoutPreparer>(enemy);
            preparer.Configure(
                loadoutSession,
                weapon,
                SoloComboTestScenario.GreatswordTrial,
                combos);
            EditorUtility.SetDirty(preparer);

            ExplorationBattleTrigger trigger = GetOrAddComponent<ExplorationBattleTrigger>(enemy);
            trigger.Configure(
                encounter,
                loadoutSession,
                encounterInstanceId,
                "CombatSandbox");
            EditorUtility.SetDirty(trigger);
            return enemy;
        }

        private static EnemyActionAuthoringData Action(
            string id,
            string displayName,
            int timelineCost,
            params EnemyResolutionEventAuthoringData[] events)
        {
            return new EnemyActionAuthoringData(id, displayName, timelineCost, events);
        }

        private static EnemySequenceAuthoringData Sequence(
            string id,
            string displayName,
            params EnemyActionAuthoringData[] actions)
        {
            return new EnemySequenceAuthoringData(id, displayName, actions);
        }

        private static EnemyResolutionEventAuthoringData Damage(
            int offset,
            float multiplier,
            DamageType damageType,
            DamageResolutionTag tags = DamageResolutionTag.None,
            ActionDisruptionDefinition onEvadedDisruption = null,
            DamageClashDefinition clash = null)
        {
            return new EnemyResolutionEventAuthoringData(
                offset,
                ActionResolutionEffect.Damage,
                multiplier,
                damageType,
                tags,
                onEvadedDisruption: onEvadedDisruption,
                clash: clash);
        }

        private static EnemyResolutionEventAuthoringData Guard(int offset)
        {
            return new EnemyResolutionEventAuthoringData(offset, ActionResolutionEffect.Guard);
        }

        private static EnemyResolutionEventAuthoringData Evade(int offset)
        {
            return new EnemyResolutionEventAuthoringData(offset, ActionResolutionEffect.Evade);
        }

        private static EncounterRewardDropAuthoringData[] CopperDrops(
            int bronzeWeight,
            int brassWeight,
            int whiteCopperWeight)
        {
            return new[]
            {
                new EncounterRewardDropAuthoringData(
                    BattleRewardSummary.BronzeMaterialId,
                    bronzeWeight),
                new EncounterRewardDropAuthoringData(
                    BattleRewardSummary.BrassMaterialId,
                    brassWeight),
                new EncounterRewardDropAuthoringData(
                    BattleRewardSummary.WhiteCopperMaterialId,
                    whiteCopperWeight)
            };
        }

        private static bool TryLoadGreatswordCombos(
            out ExplorationBattleDemoLoadoutPreparer.ComboAssetSet[] combos,
            out string validationError)
        {
            var assetsById = new Dictionary<string, ComboActionAsset>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:ComboActionAsset", new[] { ActionFolder });
            for (int index = 0; index < guids.Length; index++)
            {
                ComboActionAsset asset = AssetDatabase.LoadAssetAtPath<ComboActionAsset>(
                    AssetDatabase.GUIDToAssetPath(guids[index]));
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
                    string actionId = ids[comboIndex, slotIndex];
                    if (!assetsById.TryGetValue(actionId, out slotAssets[slotIndex]))
                    {
                        validationError = $"Missing required Action '{actionId}'.";
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
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                {
                    if (candidate.gameObject.activeInHierarchy
                        && candidate.gameObject.CompareTag("Player"))
                    {
                        return candidate.gameObject;
                    }
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (string.Equals(root.name, objectName, StringComparison.Ordinal))
                {
                    return root;
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
