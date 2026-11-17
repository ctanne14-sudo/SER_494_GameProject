using SER494.Gameplay.ExplorationMenu.Unity;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProcessingShopAction : Action
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string fallbackPrompt = "E / A  Processing Shop";
    private bool _showFallbackPrompt;

    public override void action(PlayerInteractor interactor)
    {
        if (interactor != null)
        {
            ProcessingShopController.TryOpen();
        }
    }

    public override void showUI(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);
            _showFallbackPrompt = false;
            return;
        }

        _showFallbackPrompt = show;
    }

    private void OnDisable()
    {
        _showFallbackPrompt = false;
    }

    private void OnGUI()
    {
        if (!_showFallbackPrompt || ExplorationMenuInputGate.IsOpen)
        {
            return;
        }

        const float width = 260f;
        const float height = 42f;
        Rect promptRect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - 120f,
            width,
            height);
        GUI.Box(promptRect, fallbackPrompt);
    }
}
