using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [Header("Plyaer Scripts")]
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private PlayerSpeed ps;

    [Header("Hotbar")]
    [SerializeField] private HotbarSlot[] hotbarSlots;  //List of slots in hotbar

    [Header("Hand")]
    [SerializeField] private GameObject spawnPoint; //Spawn point for held items
    [Header("Inventory Slot")]

    [SerializeField] private GameObject invSlotPrefab;  //Prefab for added slots to inventory

    [SerializeField] private List<InvSlot> invSlots = new List<InvSlot>();  //List of slots in inventory
    private List<Item> inInventory = new List<Item>();  //List of items in inventory/hotbar
    public InvSlot selectedInvSlot = null;  //Most recent selected inventory slot
    private InvSlot prevInvSlot = null;     //Previous selected inventory slot

    private bool openInventory = false;     //True if inventory is opened

    [HideInInspector] public int gold;      //Count of current gold/money

    private int hbSelected = -1;            //Index of the selected hotbar slot

    private readonly KeyCode[] numKeys = {KeyCode.Alpha1, KeyCode.Alpha2,   //Keys for hotbar selection
        KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5};

    /**
        Initializes the hotbar and inventory
    **/
    void Awake()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i].setNum(i + 1);
        }
        for (int i = 0; i < invSlots.Count; i++)
            invSlots[i].addInventory(this, i);

        addInvSlot();
    }


    void Update()
    {

        //Gets the selected key (1-5) and sets the hotbar -0 for deselect
        for (int i = 0; i < numKeys.Length; i++)
            if (Input.GetKeyDown(numKeys[i])) selectHB(i);
        if (Input.GetKeyDown(KeyCode.Alpha0)) selectHB(-1);

        //Uses the selected hotbar item
        if (Input.GetMouseButtonUp(0) && !openInventory)
            useItem();

        //Updates inventory once items have no uses left
        foreach (Item item in inInventory) 
            if (item.usesLeft <= 0) {
                removeItem(item);
            }
        
        //Opens the inventory with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            prevInvSlot = null;
            openInventory = !openInventory;
            invSlots[0].transform.root.gameObject.SetActive(openInventory);
            Cursor.lockState = openInventory ? CursorLockMode.None : CursorLockMode.Locked;
            transform.GetComponentInChildren<CameraMovement>().enabled = !openInventory;
        }

        //Swaps the selected item with the clicked item when Left Shift is held
        if (Input.GetKey(KeyCode.LeftShift) && prevInvSlot != null && selectedInvSlot != null)
        {
            swapInvSlots();
        }
        prevInvSlot = null;
    }

    /**
        Uses the item if it has an action and uses left
    **/
    void useItem()
    {
        Item item = getSelectedHotbar();
        if (item != null && item.usesLeft > 0) {
            Action action = item.prefab.GetComponent<Action>();
            if (action != null) {
                action.action(interactor);
                item.usesLeft--;
            }
        }
    }

    /**
        Selects the hotbar slot (1-5)
            - Pressing same number deselects the item (ie 1 -> 1)
    **/
    public void selectHB(int index)
    {
        hbSelected = hbSelected == index ? -1 : index;
        if (openInventory)
        {
            if (selectedInvSlot != null && hbSelected != -1)
            {
                swapWithHotbar(selectedInvSlot);
            } 
        }        
        bool select = hbSelected >= 0 && hbSelected < hotbarSlots.Length;
        for (int i = 0; i < hotbarSlots.Length; i++)
            hotbarSlots[i].setSelected(select && i == hbSelected);
        ps.changeMultiplier(select ? .8f : 1f); 
    }


    /**
        Adds item to inventory in the next available slot
    **/
    private void addToInvSlot(Item item)
    {
        bool added = false;
        while (!added) {
            addInvSlot();
            foreach (InvSlot slot in invSlots)
            {
                if (slot.getItem() == null)
                {
                    item.setInvSlot(slot);
                    slot.setItem(item);
                    added = true;
                    break;
                }
            }
        }
    }

    /**
        Adds slots to inventory
        Keeps at least 1 extra row
    **/
    private void addInvSlot()
    {
        while (invSlots.Count <= (((inInventory.Count / 6) + 2) * 6 - 1)) {
            GameObject spawn = Instantiate(invSlotPrefab, invSlots[0].transform.parent, false);
            InvSlot slot = spawn.GetComponent<InvSlot>();
            slot.addInventory(this, invSlots.Count);
            invSlots.Add(slot);
        }
    }

    /**
        Adds item to inventory if there are no matching items
        Adds uses to matching items
    **/
    public void addItem(Item item)
    {
        foreach (Item i in inInventory)
        {
            if (item.prefab == i.prefab)
            {
                i.usesLeft += item.usesLeft;
                if (i.invSlot == null)
                {
                    addToInvSlot(i);
                } 
                i.invSlot.setCount();
                return;
            }
        }
        inInventory.Add(item);
        addToInvSlot(item);
    }

    /**
        Removes items from the inventory 
    **/
    private void removeItem(Item item)
    {
        if (item == null) return;
        inInventory.Remove(item);
        foreach (HotbarSlot slot in hotbarSlots)
        {
            if (slot.getItem() == item)
            {
                slot.clear();
            }
        }
        if (item.invSlot != null) item.invSlot.clear();
    }

    /**
        Swaps the selected inventory item with the selected hotbar slot
    **/
    public void swapWithHotbar(InvSlot selectedInvSlot)
    {
        Item hbItem = hotbarSlots[hbSelected].getItem();
        Item invItem = selectedInvSlot.getItem();

        if (hbItem == null && invItem == null)
        {
            selectedInvSlot.clearSelection();
            this.selectedInvSlot = null;
            return;
        }

        if (invItem != null)
        {
            Item newItem = hotbarSlots[hbSelected].setItem(invItem, spawnPoint);
            invItem.setInvSlot(null);
            if (newItem != null)
            {
                selectedInvSlot.setItem(newItem);
            } else 
                selectedInvSlot.clear();
        } else
        {
            hotbarSlots[hbSelected].clear();
            selectedInvSlot.setItem(hbItem);
        }
        this.selectedInvSlot = null;
        clearInvSelection(null);
    }

    /**
        Swaps the two selected inventory slots
    **/
    private void swapInvSlots()
    {
            Item temp = prevInvSlot.getItem();
            if (selectedInvSlot.getItem() == null) prevInvSlot.clear();
            else prevInvSlot.setItem(selectedInvSlot.getItem());

            if (temp == null) selectedInvSlot.clear();
            else selectedInvSlot.setItem(temp);

            prevInvSlot = null;
    }

    /**
        Clears all but the given inventory slot selection
    **/
    public void clearInvSelection(InvSlot slot)
    {
        foreach (InvSlot s in invSlots)
        {
            if (s != slot) s.clearSelection();
        }
    }

    /**
        Returns selected hotbar item
    **/
    public Item getSelectedHotbar() 
        => hbSelected >= 0 && hbSelected < hotbarSlots.Length ? hotbarSlots[hbSelected].getItem() : null;

    /**
        Returns true if the a hotbar is selected
    **/
    public bool hasHBSelected() => hbSelected >= 0 && hbSelected < hotbarSlots.Length;

    /**
        Sets the selected and previous inventory slot
    **/
    public void setSelectedInvSlot(InvSlot slot)
    {
        prevInvSlot = selectedInvSlot;
        selectedInvSlot = slot;

    }
}

/**
    Public class to store an items information
**/
public class Item
{
    public string id;   //Id for keys or locks
    public GameObject prefab;   //Objects prefab model
    public Texture texture;     //Objects 2-d picture for display
    public int usesLeft;        //Amount of uses it has
    public string displayName;  //The name to be displayed
    public InvSlot invSlot = null;  //Refrence to the slot its stored in


    /**
        Creates the object
    **/
    public Item(GameObject prefab, Texture texture, int uses, string displayName, string id = null)
    {
        this.id = id;
        this.prefab = prefab;
        this.texture = texture;
        usesLeft = uses;
        this.displayName = displayName;
    }

    /**
        Set the refrence to the inventory
    **/
    public void setInvSlot(InvSlot slot) => invSlot = slot;
}