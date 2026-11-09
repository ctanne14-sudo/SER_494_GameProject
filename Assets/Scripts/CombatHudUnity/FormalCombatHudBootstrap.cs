using UnityEngine;
using UnityEngine.UIElements;

namespace SER494.Gameplay.CombatHud.Unity
{
    public static class FormalCombatHudBootstrap
    {
        private const string ObjectName = "Formal Combat HUD";
        private const string VisualTreePath = "CombatHud/FormalCombatHud";
        private const string PanelSettingsPath =
            "ExplorationMenu/ExplorationMenuPanelSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureHudExists()
        {
            FormalCombatHudController existingController =
                Object.FindAnyObjectByType<FormalCombatHudController>();
            if (existingController != null)
            {
                if (existingController.GetComponent<FormalCombatFeedbackController>() == null)
                {
                    existingController.gameObject.AddComponent<FormalCombatFeedbackController>();
                }

                return;
            }

            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(VisualTreePath);
            PanelSettings panelSettings = Resources.Load<PanelSettings>(PanelSettingsPath);
            if (visualTree == null || panelSettings == null)
            {
                Debug.LogError(
                    "Formal Combat HUD Resources are missing. Reimport "
                    + "Assets/GameData/Resources/CombatHud.");
                return;
            }

            var gameObject = new GameObject(ObjectName);
            Object.DontDestroyOnLoad(gameObject);
            UIDocument document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = visualTree;
            document.sortingOrder = 550;
            gameObject.AddComponent<FormalCombatHudController>();
            gameObject.AddComponent<FormalCombatFeedbackController>();
        }
    }
}
