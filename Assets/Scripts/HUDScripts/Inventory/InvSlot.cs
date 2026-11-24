using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InvSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RawImage icon;     //Image of the Item
    [SerializeField] private Image highlight;   //Enable When Selected
    [SerializeField] private GameObject itemPopUp;   //Enable When Hovered
    [SerializeField] private ItemDatabase db;
    private float offset = 71.5f;

    private Inventory inventory;    //Reference to players inventory
    private string itemID = null;   //The itemID held in the slot
    private bool selected = false;  //True when the item is clicked
    private bool hovering = false;


    void Update()
    {
        if (hovering)
        {
            itemPopUp.GetComponent<ItemPopUp>().setItemID(itemID);
            ((RectTransform)itemPopUp.transform).position = transform.TransformPoint(new Vector3(offset, 0f, 0f));
        }
    }

    void OnDisable()
    {
        highlight.gameObject.SetActive(selected);
        hovering = false;
    }

    /**
        Adds the reference to players inventory and slot index
    **/
    public void addInventory(Inventory inventory, bool endRow)
    {
        this.inventory = inventory;
        offset = endRow ? -offset : offset;
    }

    /**
        Function for when a slot is clicked on
        Will always highlight the most recent item
        States:
            -Hotbar item is selected -> swap with clicked item
            -Slot is selected and Left Shift is held-> swap with clicked item
            -Slot is selcted-> 1-5 num key press switches with that hotbar
    **/
    public void onClick()
    {
        inventory.clearInvSelection(this);

        if (inventory.hasHBSelected())
        {
            inventory.swapWithHotbar(this);
            inventory.selectHB(-1);
        }
        selected = !selected;
        transform.localScale = selected || hovering ? new Vector2(1.05f, 1.05f) : Vector2.one;
        highlight.gameObject.SetActive(selected || hovering);
        inventory.setSelectedInvSlot(selected ? this : null);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        itemPopUp.SetActive(true);
        highlight.gameObject.SetActive(true);
        transform.localScale = new Vector2(1.05f, 1.05f);
        itemPopUp.transform.localScale = new Vector2(1.05f, 1.05f); 
        itemPopUp.GetComponent<ItemPopUp>().setItemID(null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        itemPopUp.SetActive(false);
        highlight.gameObject.SetActive(selected);
        itemPopUp.GetComponent<ItemPopUp>().setItemID(null);
        transform.localScale = selected ? new Vector2(1.05f, 1.05f) : Vector2.one;
    }

    /**
        Sets the slots item
    **/
    public void setItemID(string itemID)
    {
        this.itemID = itemID;
        ItemData data = string.IsNullOrEmpty(itemID) ? null : db.Get(itemID);
        icon.texture = data != null ? data.icon : null;
        icon.enabled = data != null;    
        if (inventory.selectedInvSlot != this)
            clearSelection();
    }

    /**
        Helper to clear slots selection
    **/
    public void clearSelection()
    {
        selected = false;
        highlight.gameObject.SetActive(selected);        
        transform.localScale = selected ? new Vector2(1.05f, 1.05f) : Vector2.one;
    }
    
    /**
        Clears the slot completly
    **/
    public void clear()
    {
        itemID = null;
        icon.texture = null;
        icon.enabled = false;
        selected = false;
        if (inventory.selectedInvSlot != this)
            clearSelection();
    }

    /**
        Returns the currently stored item
    **/
    public string getItemID() => itemID;
}
