using SER494.Gameplay.ExplorationMenu.Unity;
using SER494.Gameplay.PlayerState.Unity;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PersistentWorldObject))]
public sealed class PersistentItemPickupAction : Action
{
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private string fallbackPrompt = "E / A  Pick Up Healing Potion";
    [SerializeField] private string itemId = PlayerStateInitialValues.HealingPotionId;
    [Min(1)]
    [SerializeField] private int amount = 1;

    private PersistentWorldObject _persistentWorldObject;
    private bool _showFallbackPrompt;

    private void Awake()
    {
        _persistentWorldObject = GetComponent<PersistentWorldObject>();
    }

    public void Configure(
        string configuredItemId,
        int configuredAmount,
        string configuredPrompt = null)
    {
        if (string.IsNullOrWhiteSpace(configuredItemId))
        {
            throw new System.ArgumentException(
                "An item ID is required.",
                nameof(configuredItemId));
        }

        if (configuredAmount <= 0)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(configuredAmount),
                configuredAmount,
                "Pickup amount must be positive.");
        }

        itemId = configuredItemId.Trim();
        amount = configuredAmount;
        if (!string.IsNullOrWhiteSpace(configuredPrompt))
        {
            fallbackPrompt = configuredPrompt.Trim();
        }
    }

    public override void action(PlayerInteractor interactor)
    {
        if (interactor == null
            || !PlayerStateService.TryGetActive(out PlayerStateService stateService)
            || !PlayerSaveService.TryGetActive(out _))
        {
            Debug.LogError(
                $"Cannot collect '{name}' because the active Player State or Save service is unavailable.",
                this);
            return;
        }

        stateService.State.Inventory.Add(itemId, amount);
        _showFallbackPrompt = false;
        _persistentWorldObject.MarkCompleted();
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

        const float width = 280f;
        const float height = 42f;
        Rect promptRect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - 120f,
            width,
            height);
        GUI.Box(promptRect, fallbackPrompt);
    }
}
