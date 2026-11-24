using UnityEngine;
using System.Collections.Generic;
using SER494.Gameplay.ExplorationMenu.Unity;
using SER494.Gameplay.PlayerState.Unity;
using PersistentPlayerState = SER494.Gameplay.PlayerState.PlayerState;
using SER494.Gameplay.PlayerState;
using System;

public class Inventory : MonoBehaviour
{
    [Header("Plyaer Scripts")]
    private PlayerInventoryState inventory;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private ItemDatabase db;
    [SerializeField] private PlayerSpeed ps;

    [Header("Hotbar")]
    [SerializeField] private HotbarSlot[] hotbarSlots;  //List of slots in hotbar
    public int rowCount;

    [Header("Hand")]
    [SerializeField] private GameObject spawnPoint; //Spawn point for held items

    [SerializeField] private GameObject inventoryPanel;  //Initial panel of inventory
    private GameObject invSlotObject;   //Initial Object to clone for added slots to inventory

    private List<InvSlot> invSlots = new List<InvSlot>();  //List of slots in inventory
    [HideInInspector] public InvSlot selectedInvSlot = null;  //Most recent selected inventory slot
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
        invSlotObject = inventoryPanel.GetComponentInChildren<InvSlot>().gameObject;
        invSlots.Add(invSlotObject.GetComponent<InvSlot>());

        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i].setNum(i + 1);
        }
        for (int i = 0; i < invSlots.Count; i++)
        {
            bool endRow = (i % 8) > 5;
            invSlots[i].addInventory(this, endRow);
        }


        if (PlayerStateService.TryGetActive(out PlayerStateService service)
                && service.State != null)
            {
                PersistentPlayerState state = service.State;
                inventory = state.Inventory;

            foreach (KeyValuePair<string, int> key in inventory.ItemCounts)
            {
                addToInvSlot(key.Key);
            }
            }
    }


    void Update()
    {
        if (ExplorationMenuInputGate.IsOpen)
        {
            return;
        }

        //Gets the selected key (1-5) and sets the hotbar -0 for deselect
        for (int i = 0; i < numKeys.Length; i++)
            if (Input.GetKeyDown(numKeys[i])) selectHB(i);
        if (Input.GetKeyDown(KeyCode.Alpha0)) selectHB(-1);

        //Uses the selected hotbar item
        if (Input.GetMouseButtonUp(0) && !openInventory)
            useItem();

        //Opens the inventory with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            prevInvSlot = null;
            openInventory = !openInventory;
            inventoryPanel.SetActive(openInventory);
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
        string itemID = getSelectedHotbar();
        if (!string.IsNullOrEmpty(itemID)) {
            Action action = db.Get(hotbarSlots[hbSelected].getItemID()).prefab.GetComponent<Action>();
            if (action != null)
            {
                if (inventory.TryConsume(itemID, 1)) 
                {
                    action.action(interactor);
                    if (inventory.GetCount(itemID) <= 0)
                        hotbarSlots[hbSelected].clear();
                }
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
    public void addToInvSlot(string itemID)
    {
        bool added = false;
        while (!added) {
            addInvSlot();
            foreach (InvSlot slot in invSlots)
            {
                if (slot.getItemID() == null)
                {
                    slot.setItemID(itemID);
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
        while (invSlots.Count <= (((inventory.ItemCounts.Count / 8) + 2) * 8 - 1)) {
            GameObject spawn = Instantiate(invSlotObject);
            spawn.transform.SetParent(invSlotObject.transform.parent);
            spawn.transform.localScale = Vector3.one;
            InvSlot slot = spawn.GetComponent<InvSlot>();
            invSlots.Add(slot);
            int index = invSlots.IndexOf(slot);
            bool endRow = (index % 8) > 5;
            slot.addInventory(this, endRow);
            slot.setItemID(null);
        }
        rowCount = (int)Mathf.Ceil(invSlots.Count / 8);
        RectTransform rect = (RectTransform)invSlots[0].transform.parent.transform;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 44 + rowCount * 35 + ((rowCount -1) * 25) );
    }

    /**
        Swaps the selected inventory item with the selected hotbar slot
    **/
    public void swapWithHotbar(InvSlot selectedInvSlot)
    {
        string hbItem = hotbarSlots[hbSelected].getItemID();
        string invItem = selectedInvSlot.getItemID();

        if (hbItem == null && invItem == null)
        {
            selectedInvSlot.clearSelection();
            this.selectedInvSlot = null;
            return;
        }

        if (!string.IsNullOrEmpty(invItem))
        {
            string newItem = hotbarSlots[hbSelected].setItemID(invItem, spawnPoint);
            if (!string.IsNullOrEmpty(newItem))
            {
                selectedInvSlot.setItemID(newItem);
            } else 
                selectedInvSlot.clear();
        } else
        {
            hotbarSlots[hbSelected].clear();
            selectedInvSlot.setItemID(hbItem);
        }
        this.selectedInvSlot = null;
        clearInvSelection(null);
    }

    /**
        Swaps the two selected inventory slots
    **/
    private void swapInvSlots()
    {
        string a = prevInvSlot.getItemID();
        string b = selectedInvSlot.getItemID();
        prevInvSlot.setItemID(b);
        selectedInvSlot.setItemID(a);
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
    public string getSelectedHotbar() 
        => hbSelected >= 0 && hbSelected < hotbarSlots.Length ? hotbarSlots[hbSelected].getItemID() : null;

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

    internal void addItem(string id, int uses)
    {
        bool alreadyShown = invSlots.Exists(s => s.getItemID() == id)
        || Array.Exists(hotbarSlots, h => h.getItemID() == id);
        inventory.Add(id, uses);
        if (!alreadyShown) addToInvSlot(id);
    }
}