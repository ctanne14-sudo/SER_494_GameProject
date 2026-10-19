using UnityEngine;

public class FloorExit : MonoBehaviour
{
    public bool isOpen = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CharacterController>() == null) return;

        if (!isOpen)
        {
            GameManager.Instance.ShowMessage("The stairs are locked. Clear the floor first");
            return;
        }

        GameManager.Instance.NextFloor();
    }

    public void Open()
    {
        isOpen = true;
        GameManager.Instance.ShowMessage("The stairs are open!");
    }
}
