using UnityEngine;

public class MoneyAction : Action
{
    
    public override void action(PlayerInteractor interactor)
    {
        interactor.inventory.gold += 50;
    }

    public override string setUiPrompt()
    {
        return "50 Gold";
    }
}