using UnityEngine;
using UnityEngine.UIElements;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    public static class ExplorationMenuBootstrap
    {
        private const string ObjectName = "Exploration Menu";
        private const string VisualTreePath = "ExplorationMenu/ExplorationMenu";
        private const string PanelSettingsPath = "ExplorationMenu/ExplorationMenuPanelSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMenuExists()
        {
            if (Object.FindAnyObjectByType<ExplorationMenuController>() != null)
            {
                return;
            }

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(VisualTreePath);
            PanelSettings panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError(
                    "Exploration Menu Resources are missing. Reimport Assets/GameData/Resources/ExplorationMenu.");
                return;
            }

            var gameObject = new GameObject(ObjectName);
            Object.DontDestroyOnLoad(gameObject);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 500;
            gameObject.AddComponent<ExplorationMenuController>();
        }
    }
}
