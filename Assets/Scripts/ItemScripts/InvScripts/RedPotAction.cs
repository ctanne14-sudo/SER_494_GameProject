using UnityEngine;

public class RedPotAction : Action
{
    
    public override void action(PlayerInteractor interactor)
    {
        interactor.hc.increaseMaxHp(50);
    }

    public override void showUI(bool show) {}
}