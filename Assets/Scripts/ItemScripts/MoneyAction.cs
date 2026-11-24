using SER494.Gameplay.PlayerState.Unity;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;

public class MoneyAction : Action
{
    
    public override void action(PlayerInteractor interactor)
    {
        if (PlayerStateService.TryGetActive(out PlayerStateService service)
                || service.State != null)
            {
                PersistentPlayerState state = service.State;
                state.Currencies.Add("currency.gold", 50);
            }
    }

    public override void showUI(bool show) {}
}