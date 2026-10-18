using UnityEngine;

public class PickUp : Action
{
    public override void action(PlayerInteractor interactor)
    {
        Destroy(gameObject);
    }

    public override string setUiPrompt()
    {
        return "[E] Pick Up";
    }
}