using UnityEngine;

public abstract class Action : MonoBehaviour
{
    [SerializeField] private GameObject offset;
    public Transform Offset => offset != null ? offset.transform : null;
    public abstract void action(PlayerInteractor interactor);
    public abstract string setUiPrompt();
    public abstract void showUI(bool show);
}