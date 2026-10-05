using System;
using System.Collections.Generic;
using SER494.Gameplay.Combat.Authoring.SoloCombo;
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
        private const string PanelSettingsPath = "Assets/UI/SoloCombo/ComboBuilderPanelSettings.asset";
        private const string VisualTreePath = "Assets/UI/SoloCombo/SoloComboBuilder.uxml";
        private const string SampleActionFolder = "Assets/GameData/SoloCombo/SampleActions";

        [MenuItem("Tools/SER494/Create Combo Builder Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            PanelSettings panelSettings = GetOrCreatePanelSettings();
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(VisualTreePath);
            if (visualTree == null)
            {
                Debug.LogError($"Could not load Combo Builder UXML at {VisualTreePath}.");
                return;
            }

            ComboActionAsset[] sampleActions = LoadSampleActions();

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
            ConfigureController(controller, sampleActions);
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

            Selection.activeGameObject = builderObject;
            EditorGUIUtility.PingObject(builderObject);
            Debug.Log(
                $"Created or repaired Combo Builder prototype scene at {ScenePath} with "
                + $"{sampleActions.Length} sample Action asset(s).");
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
            ComboActionAsset[] sampleActions)
        {
            var serializedController = new SerializedObject(controller);
            SerializedProperty actionArray = serializedController.FindProperty("availableActions");
            actionArray.arraySize = sampleActions.Length;

            for (int index = 0; index < sampleActions.Length; index++)
            {
                actionArray.GetArrayElementAtIndex(index).objectReferenceValue = sampleActions[index];
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
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

        private static ComboActionAsset[] LoadSampleActions()
        {
            string[] guids = AssetDatabase.FindAssets("t:ComboActionAsset", new[] { SampleActionFolder });
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
                    Debug.LogWarning($"Could not load sample Combo Action at {assetPath}.");
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
