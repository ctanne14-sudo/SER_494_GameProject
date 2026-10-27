using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    public static class TitleMenuBootstrap
    {
        public const string TitleSceneName = "TitleMenu";
        public const string ExplorationSceneName = "Floor1_Cell_A";

        private const string ObjectName = "Title Menu";
        private const string CameraObjectName = "Title Camera";
        private const string VisualTreePath = "TitleMenu/TitleMenu";
        private const string PanelSettingsPath = "ExplorationMenu/ExplorationMenuPanelSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoadedHandler()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMenuExists()
        {
            EnsureTitleScenePresentation(SceneManager.GetActiveScene());
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            EnsureTitleScenePresentation(scene);
        }

        private static void EnsureTitleScenePresentation(Scene scene)
        {
            if (!string.Equals(scene.name, TitleSceneName, System.StringComparison.Ordinal))
            {
                return;
            }

            EnsureTitleCamera(scene);
            if (Object.FindAnyObjectByType<TitleMenuController>() != null)
            {
                return;
            }

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(VisualTreePath);
            PanelSettings panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError(
                    "Title Menu Resources are missing. Reimport Assets/GameData/Resources/TitleMenu.");
                return;
            }

            var gameObject = new GameObject(ObjectName);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 1000;
            gameObject.AddComponent<TitleMenuController>();
        }

        private static void EnsureTitleCamera(Scene titleScene)
        {
            GameObject existing = GameObject.Find(CameraObjectName);
            if (existing != null && existing.scene == titleScene)
            {
                return;
            }

            var cameraObject = new GameObject(CameraObjectName);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(15, 20, 26, 255);
            camera.cullingMask = 0;
            camera.depth = -100f;
        }
    }
}
