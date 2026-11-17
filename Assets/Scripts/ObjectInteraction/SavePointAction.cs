using SER494.Gameplay.ExplorationMenu.Unity;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SavePointAction : Action
{
    [Tooltip("Optional world-space prompt, such as an '[E] Save' label.")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string fallbackPrompt = "E / A  Save";
    [Tooltip("Stable ID used by save files. Do not change it after release.")]
    [SerializeField] private string savePointId = "save-point";
    private bool _showFallbackPrompt;

    public override void action(PlayerInteractor interactor)
    {
        if (interactor == null)
        {
            return;
        }

        SavePointMenuController.TryOpen(new SavePointContext(
            SceneManager.GetActiveScene().name,
            savePointId,
            interactor.transform.position,
            interactor.transform.rotation));
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

        const float width = 190f;
        const float height = 42f;
        Rect promptRect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - 120f,
            width,
            height);
        GUI.Box(promptRect, fallbackPrompt);
    }
}
