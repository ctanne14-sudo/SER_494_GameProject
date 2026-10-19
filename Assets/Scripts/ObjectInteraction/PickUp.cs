using UnityEngine;

public class PickUp : WorldAction
{
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private GameObject itemPrefab;

    [SerializeField] private Texture itemImage;

    public override void action(PlayerInteractor interactor)
    {
        interactor.inventory.addItem(itemPrefab, itemImage);
        Destroy(gameObject);
    }

    public override string setUiPrompt()
    {
        return "[E] Pick Up";
    }
}