using UnityEngine;

public class ConsumablePickUp : PickUp

{
    [SerializeField] private Action itemAction;

    protected override void pickUp(PlayerInteractor interactor)
        => itemAction.action(interactor);
}