using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class InvSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RawImage icon;     //Image of the Item
    [SerializeField] private Image highlight;   //Enable When Selected
    [SerializeField] private GameObject itemPopUp;   //Enable When Hovered
    private float offset = 71.5f;

    private Inventory inventory;    //Reference to players inventory
    private Item item;      //The item held in the slot
    private bool selected = false;  //True when the item is clicked
    private bool hovering = false;


    void Update()
    {
        if (hovering)
        {
            itemPopUp.GetComponent<ItemPopUp>().setItem(item);
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
        Debug.Log("Offset: " + offset + " " + endRow);

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
        highlight.gameObject.SetActive(selected || hovering);
        inventory.setSelectedInvSlot(selected ? this : null);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        itemPopUp.SetActive(true);
        highlight.gameObject.SetActive(true);
        transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
        itemPopUp.transform.localScale = new Vector2(1.05f, 1.05f); 
        itemPopUp.GetComponent<ItemPopUp>().setItem(null);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        itemPopUp.SetActive(false);
        highlight.gameObject.SetActive(selected);
        itemPopUp.GetComponent<ItemPopUp>().setItem(null);
        transform.localScale = Vector2.one;
    }

    /**
        Sets the slots item
    **/
    public void setItem(Item item)
    {
        this.item = item;
        if (item != null) {
            item.setInvSlot(this); 
            icon.texture = item.texture;
            icon.enabled = true;    
        } else
        {
            icon.enabled = false;
        }
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
    }
    
    /**
        Clears the slot completly
    **/
    public void clear()
    {
        item = null;
        icon.texture = null;
        icon.enabled = false;
        selected = false;
        if (inventory.selectedInvSlot != this)
            clearSelection();
    }

    /**
        Returns the currently stored item
    **/
    public Item getItem() => item;
}
