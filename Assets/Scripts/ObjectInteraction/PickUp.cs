using UnityEngine;

public class PickUp : Action
{
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Texture itemImage;

    public override void action(PlayerInteractor interactor)
    {
        if (gameObject.CompareTag("InventoryItem")) {
            interactor.inventory.addItem(itemPrefab, itemImage);
        } else
        {
            Action itemAction = itemPrefab.GetComponent<Action>();
            itemAction.action(interactor);
        }
        Destroy(gameObject);
    }

    public override string setUiPrompt()
    {
        return "[E] Pick Up";
    }
}