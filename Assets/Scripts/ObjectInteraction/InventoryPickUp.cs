using UnityEngine;

public class InventoryPickUp : PickUp

{
    [Header("Object")]
    [SerializeField] private int uses;
    [SerializeField] private string id;

    protected override void pickUp(PlayerInteractor interactor) 
        => interactor.inventory.addItem(id, uses);

}