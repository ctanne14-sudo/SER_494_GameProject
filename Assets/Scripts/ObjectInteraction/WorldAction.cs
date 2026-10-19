using UnityEngine;

public abstract class WorldAction : MonoBehaviour
{
    public abstract void action(PlayerInteractor interactor);


    public abstract string setUiPrompt();
}