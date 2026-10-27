using UnityEngine;
using UnityEngine.UIElements;

namespace SER494.Gameplay.ExplorationMenu.Unity
{
    public static class SavePointMenuBootstrap
    {
        private const string ObjectName = "Save Point Menu";
        private const string VisualTreePath = "SavePointMenu/SavePointMenu";
        private const string PanelSettingsPath = "ExplorationMenu/ExplorationMenuPanelSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMenuExists()
        {
            if (Object.FindAnyObjectByType<SavePointMenuController>() != null)
            {
                return;
            }

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(VisualTreePath);
            PanelSettings panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError(
                    "Save Point Menu Resources are missing. Reimport Assets/GameData/Resources/SavePointMenu.");
                return;
            }

            var gameObject = new GameObject(ObjectName);
            Object.DontDestroyOnLoad(gameObject);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 510;
            gameObject.AddComponent<SavePointMenuController>();
        }
    }
}
