using UnityEngine;

public abstract class Action : MonoBehaviour
{
    public abstract void action(PlayerInteractor interactor);
    public abstract void showUI(bool show);
}