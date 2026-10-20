using UnityEngine;

public class RedPotAction : Action
{
    
    public override void action(PlayerInteractor interactor)
    {
        interactor.hc.increaseMaxHp(50);
    }

    public override string setUiPrompt()
    {
        return "Red Potion";
    }

    public override void showUI(bool show) {}
}