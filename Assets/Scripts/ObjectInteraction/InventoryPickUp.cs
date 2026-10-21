using UnityEngine;

public class InventoryPickUp : PickUp

{
    [Header("Object")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Texture itemImage;
    [SerializeField] private int uses;
    [SerializeField] private string id;

    protected override void pickUp(PlayerInteractor interactor)
        => interactor.inventory.addItem(new Item(itemPrefab, itemImage, uses, displayName, id));

}