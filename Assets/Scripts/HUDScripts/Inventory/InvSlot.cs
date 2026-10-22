using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class InvSlot : MonoBehaviour
{
    [SerializeField] private RawImage icon;     //Image of the Item
    [SerializeField] private Image highlight;   //Enable When Selected
    [SerializeField] private TextMeshProUGUI itemName;  //Text field of the Item
    [SerializeField] private TextMeshProUGUI usesLeft;  //Text for the number of uses left
    private Inventory inventory;    //Reference to players inventory
    private Item item;      //The item held in the slot
    private bool selected = false;  //True when the item is clicked

    /**
        Inistializes the slots name and uses field
    **/
    void Awake()
    {
        setName();
        setCount();
    }

    /**
        Adds the reference to players inventory and slot index
    **/
    public void addInventory(Inventory inventory, int invSlotIndex)
    {
        this.inventory = inventory;
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
        highlight.gameObject.SetActive(selected);
        inventory.setSelectedInvSlot(selected ? this : null);
    }

    /**
        Sets the slots item
    **/
    public void setItem(Item item)
    {
        item.setInvSlot(this); 
        this.item = item;
        icon.texture = item.texture;
        icon.enabled = item.texture != null;
        if (inventory.selectedInvSlot != this)
            clearSelection();
        setName();
        setCount();
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
        setName();
        setCount();
    }

    /**
        Sets the Name text field
    **/
    public void setName() =>
        itemName.text = item != null ? item.displayName : "";
    
    /**
        Sets the Num uses text field
    **/
    public void setCount() =>
        usesLeft.text = item != null ? item.usesLeft.ToString() : "";
    
    /**
        Returns the currently stored item
    **/
    public Item getItem() => item;
}
