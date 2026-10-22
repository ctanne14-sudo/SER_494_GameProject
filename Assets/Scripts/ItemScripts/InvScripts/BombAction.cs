using UnityEngine;

public class BombAction : Action
{

    public override void action(PlayerInteractor interactor)
    {
        interactor.hc.damagePlayer(50);
    }

    public override void showUI(bool show) {}
}