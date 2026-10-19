using UnityEngine;

public class MoneyAction : Action
{
    
    public override void action(PlayerInteractor interactor)
    {
        interactor.hc.healPlayer(50);
    }

    public override string setUiPrompt()
    {
        return "";
    }

}