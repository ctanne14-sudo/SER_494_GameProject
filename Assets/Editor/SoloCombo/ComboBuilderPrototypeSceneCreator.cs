using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
using SER494.Gameplay.Combat.Authoring.Weapons;
using SER494.Gameplay.Combat.UI.SoloCombo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SER494.Gameplay.Combat.Editor.SoloCombo
{
    public static class ComboBuilderPrototypeSceneCreator
    {
        private const string ScenePath = "Assets/Scenes/ComboBuilderSandbox.unity";
        private const string CombatScenePath = "Assets/Scenes/CombatSandbox.unity";
        private const string PanelSettingsPath = "Assets/UI/SoloCombo/ComboBuilderPanelSettings.asset";
        private const string LoadoutSessionPath = "Assets/GameData/SoloCombo/SoloComboLoadoutSession.asset";
        private const string VisualTreePath = "Assets/UI/SoloCombo/SoloComboBuilder.uxml";
        private const string WeaponActionFolder =
            "Assets/GameData/SoloCombo/WeaponActions";

        [MenuItem("Tools/SER494/Create Combo Builder Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            SoloComboLoadoutSession loadoutSession = GetOrCreateLoadoutSession();
            if (loadoutSession == null)
            {
                Debug.LogError($"Could not load runtime loadout session at {LoadoutSessionPath}.");
                return;
            }

            PanelSettings panelSettings = GetOrCreatePanelSettings();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(VisualTreePath);
            if (visualTree == null)
            {
                Debug.LogError($"Could not load Combo Builder UXML at {VisualTreePath}.");
                return;
            }

            ComboActionAsset[] availableActions = LoadAvailableActions();
            WeaponDefinitionAsset[] sampleWeapons =
                SampleWeaponAssetCreator.SynchronizeSampleWeapons();

            SceneAsset existingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Scene scene = existingScene == null
                ? EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)
                : EditorSceneManager.OpenScene(ScenePath);

            GameObject builderObject = FindBuilderObject(scene);
            if (builderObject == null)
            {
                builderObject = new GameObject("Solo Combo Builder");
                Undo.RegisterCreatedObjectUndo(builderObject, "Create Solo Combo Builder");
                SceneManager.MoveGameObjectToScene(builderObject, scene);
            }

            UIDocument document = GetOrAddComponent<UIDocument>(builderObject);
            ConfigureDocument(document, panelSettings, visualTree);

            SoloComboBuilderController controller =
                GetOrAddComponent<SoloComboBuilderController>(builderObject);
            ConfigureController(controller, availableActions, sampleWeapons, loadoutSession);
            EnsureCamera(scene);

            if (document.panelSettings == null)
            {
                Debug.LogError(
                    $"Failed to assign Panel Settings from {PanelSettingsPath}. "
                    + "The Combo Builder scene was not saved.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            ConfigureCombatSandbox(loadoutSession);
            EnsurePrototypeScenesInBuildSettings();
            AssetDatabase.SaveAssets();
            scene = EditorSceneManager.OpenScene(ScenePath);
            builderObject = FindBuilderObject(scene);

            Selection.activeGameObject = builderObject;
            EditorGUIUtility.PingObject(builderObject);
            Debug.Log(
                $"Created or repaired Combo Builder prototype scene at {ScenePath} with "
                + $"{availableActions.Length} physical/common Action asset(s) and "
                + $"{sampleWeapons.Length} Weapon asset(s).");
        }

        private static void ConfigureDocument(
            UIDocument document,
            PanelSettings panelSettings,
            VisualTreeAsset visualTree)
        {
            var serializedDocument = new SerializedObject(document);
            serializedDocument.FindProperty("m_PanelSettings").objectReferenceValue = panelSettings;
            serializedDocument.FindProperty("sourceAsset").objectReferenceValue = visualTree;
            serializedDocument.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(document);
        }

        private static void ConfigureController(
            SoloComboBuilderController controller,
            ComboActionAsset[] sampleActions,
            WeaponDefinitionAsset[] sampleWeapons,
            SoloComboLoadoutSession loadoutSession)
        {
            Undo.RecordObject(controller, "Assign Solo Combo Loadout Session");
            controller.LoadoutSession = loadoutSession;

            var serializedController = new SerializedObject(controller);
            SerializedProperty actionArray = serializedController.FindProperty("availableActions");
            actionArray.arraySize = sampleActions.Length;
            SerializedProperty weaponArray = serializedController.FindProperty("availableWeapons");
            weaponArray.arraySize = sampleWeapons.Length;

            // Persist array resizing before assigning references. Unity can otherwise retain the
            // new size while discarding most object references when an existing scene array grows.
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            serializedController.Update();

            actionArray = serializedController.FindProperty("availableActions");
            for (int index = 0; index < sampleActions.Length; index++)
            {
                actionArray.GetArrayElementAtIndex(index).objectReferenceValue = sampleActions[index];
            }

            weaponArray = serializedController.FindProperty("availableWeapons");
            for (int index = 0; index < sampleWeapons.Length; index++)
            {
                weaponArray.GetArrayElementAtIndex(index).objectReferenceValue = sampleWeapons[index];
            }

            WeaponDefinitionAsset defaultWeapon = FindWeapon(
                sampleWeapons,
                "weapon.sword-and-shield");
            serializedController.FindProperty("equippedWeapon").objectReferenceValue =
                defaultWeapon ?? (sampleWeapons.Length > 0 ? sampleWeapons[0] : null);

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            serializedController.Update();

            if (!HasAllReferences(
                    serializedController.FindProperty("availableActions"),
                    sampleActions.Length))
            {
                Debug.LogError(
                    "Failed to assign every physical/common Action asset to "
                    + "SoloComboBuilderController.");
            }

            if (!HasAllReferences(
                    serializedController.FindProperty("availableWeapons"),
                    sampleWeapons.Length))
            {
                Debug.LogError(
                    "Failed to assign every Weapon asset to SoloComboBuilderController.");
            }

            EditorUtility.SetDirty(controller);

            if (controller.LoadoutSession == null)
            {
                Debug.LogError("Failed to assign the runtime loadout session to SoloComboBuilderController.");
            }
        }

        private static bool HasAllReferences(
            SerializedProperty array,
            int expectedCount)
        {
            if (array == null || array.arraySize != expectedCount)
            {
                return false;
            }

            for (int index = 0; index < array.arraySize; index++)
            {
                if (array.GetArrayElementAtIndex(index).objectReferenceValue == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static WeaponDefinitionAsset FindWeapon(
            IReadOnlyList<WeaponDefinitionAsset> weapons,
            string weaponId)
        {
            for (int index = 0; index < weapons.Count; index++)
            {
                WeaponDefinitionAsset weapon = weapons[index];
                if (weapon != null
                    && string.Equals(weapon.WeaponId, weaponId, StringComparison.Ordinal))
                {
                    return weapon;
                }
            }

            return null;
        }

        private static void ConfigureCombatSandbox(SoloComboLoadoutSession loadoutSession)
        {
            SceneAsset combatSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(CombatScenePath);
            if (combatSceneAsset == null)
            {
                Debug.LogError($"Could not find Combat Sandbox scene at {CombatScenePath}.");
                return;
            }

            Scene combatScene = EditorSceneManager.OpenScene(CombatScenePath);
            MonoBehaviour controller = FindCombatSandboxController(combatScene);
            if (controller == null)
            {
                Debug.LogError("CombatSandbox scene has no CombatSandboxController component.");
                return;
            }

            System.Reflection.PropertyInfo sessionProperty =
                controller.GetType().GetProperty("LoadoutSession");
            if (sessionProperty == null || !sessionProperty.CanWrite)
            {
                Debug.LogError("CombatSandboxController has no writable LoadoutSession property.");
                return;
            }

            Undo.RecordObject(controller, "Assign Solo Combo Loadout Session");
            sessionProperty.SetValue(controller, loadoutSession);
            EditorUtility.SetDirty(controller);

            if (sessionProperty.GetValue(controller) as SoloComboLoadoutSession == null)
            {
                Debug.LogError("Failed to assign the runtime loadout session to CombatSandboxController.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(combatScene);
            EditorSceneManager.SaveScene(combatScene, CombatScenePath);
        }

        private static MonoBehaviour FindCombatSandboxController(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                MonoBehaviour[] behaviours = roots[rootIndex].GetComponentsInChildren<MonoBehaviour>(true);
                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; behaviourIndex++)
                {
                    MonoBehaviour behaviour = behaviours[behaviourIndex];
                    if (behaviour != null
                        && behaviour.GetType().FullName
                            == "SER494.Gameplay.CombatSandbox.CombatSandboxController")
                    {
                        return behaviour;
                    }
                }
            }

            return null;
        }

        private static void EnsurePrototypeScenesInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddBuildSceneIfMissing(scenes, ScenePath);
            AddBuildSceneIfMissing(scenes, CombatScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddBuildSceneIfMissing(
            ICollection<EditorBuildSettingsScene> scenes,
            string scenePath)
        {
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        private static GameObject FindBuilderObject(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].name == "Solo Combo Builder")
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(gameObject);
        }

        private static void EnsureCamera(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index].GetComponentInChildren<Camera>(true) != null)
                {
                    return;
                }
            }

            var cameraObject = new GameObject("Main Camera");
            Undo.RegisterCreatedObjectUndo(cameraObject, "Create Combo Builder Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f, 1f);
        }

        private static PanelSettings GetOrCreatePanelSettings()
        {
            PanelSettings existingSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existingSettings != null)
            {
                return existingSettings;
            }

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.name = "ComboBuilderPanelSettings";
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        }

        private static SoloComboLoadoutSession GetOrCreateLoadoutSession()
        {
            SoloComboLoadoutSession existingSession =
                AssetDatabase.LoadAssetAtPath<SoloComboLoadoutSession>(LoadoutSessionPath);
            if (existingSession != null)
            {
                return existingSession;
            }

            SoloComboLoadoutSession session = ScriptableObject.CreateInstance<SoloComboLoadoutSession>();
            session.name = "SoloComboLoadoutSession";
            AssetDatabase.CreateAsset(session, LoadoutSessionPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                LoadoutSessionPath,
                ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<SoloComboLoadoutSession>(LoadoutSessionPath);
        }

        private static ComboActionAsset[] LoadAvailableActions()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:ComboActionAsset",
                new[] { WeaponActionFolder });
            var actions = new List<ComboActionAsset>(guids.Length);

            for (int index = 0; index < guids.Length; index++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[index]);
                ComboActionAsset action = AssetDatabase.LoadAssetAtPath<ComboActionAsset>(assetPath);
                if (action != null)
                {
                    actions.Add(action);
                }
                else
                {
                    Debug.LogWarning($"Could not load Combo Action at {assetPath}.");
                }
            }

            actions.Sort(CompareActions);
            return actions.ToArray();
        }

        private static int CompareActions(ComboActionAsset left, ComboActionAsset right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int categoryComparison = left.Category.CompareTo(right.Category);
            return categoryComparison != 0
                ? categoryComparison
                : string.Compare(left.ActionId, right.ActionId, StringComparison.Ordinal);
        }
    }
}
