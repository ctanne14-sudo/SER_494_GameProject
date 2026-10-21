using UnityEngine;

public class HeartAction : Action
{

    public override void action(PlayerInteractor interactor)
    {
        interactor.hc.healPlayer(50);
    }

    public override void showUI(bool show) {}
}