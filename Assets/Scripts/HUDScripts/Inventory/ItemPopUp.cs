using UnityEngine;
using TMPro;
using SER494.Gameplay.PlayerState.Unity;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;
using SER494.Gameplay.PlayerState;
using Unity.VisualScripting;

public class ItemPopUp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usesLeft;
    [SerializeField] private TextMeshProUGUI displayName;
    [SerializeField] private TextMeshProUGUI action;
    [SerializeField] private ItemDatabase db;

    private string itemID = null;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
        }
        if (!string.IsNullOrEmpty(itemID))
        {
            if (PlayerStateService.TryGetActive(out PlayerStateService service)
                && service.State != null)
            {
                usesLeft.text = "x" + service.State.Inventory.GetCount(itemID);
                ItemData data = db.Get(itemID);
                displayName.text = data != null ? data.displayName : itemID;
                action.text = data != null ? data.action : "";
            }
        } else
        {
            usesLeft.text = "x0";
            displayName.text = "";
            action.text = "";
        }
    }

    void OnDisable()
    {
        gameObject.SetActive(false);       
    }

    public void setItemID(string itemID) => this.itemID = itemID;
}
