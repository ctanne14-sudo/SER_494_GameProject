using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [SerializeField] private ContentController content;
    [SerializeField] private HotbarSlot[] slots;
    [SerializeField] private PlayerInteractor interactor;

    [SerializeField] private PlayerSpeed ps;
    [SerializeField] private GameObject spawnPoint;

    public   List<Item> inInventory = new List<Item>();

    [HideInInspector] public int gold;

    private int selected = -1;

    private readonly KeyCode[] numKeys = {KeyCode.Alpha1, KeyCode.Alpha2, 
        KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5};

    private readonly KeyCode[] inputKeys = {KeyCode.E, KeyCode.Mouse0};
    
    void Start()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].setNum(i + 1);
        }
    }
    void Update()
    {
        for (int i = 0; i < numKeys.Length; i++)
        {
            if (Input.GetKeyDown(numKeys[i])) select(i);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) select(-1);

        if (Input.GetMouseButtonUp(0))
            useItem();

        for (int i = 0; i < inInventory.Count; i++)
        {
            if (inInventory[i].usesLeft <= 0)
            {
                removeItem(i);
                i--;
            }
        }
    }

    void useItem()
    {
        Item item = getSelected();
        if (item != null) {
            Action action = item.prefab.GetComponent<Action>();
            if (action != null) {
                action.action(interactor);
                item.usesLeft--;
            }
        }
    }

    public bool isInSlot(int invIndex)
    {
        for (int i = 0; i < slots.Length; i++) 
            if (invIndex == slots[i].getInvIndex()) return true;
        return false;
    }

    public void select(int index)
    {
        if (selected == index) index = -1;
        selected = index;
        bool select = selected >= 0 && selected < slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slots[i].setSelected(select && i == selected);
        ps.changeMultiplier(select ? .8f : 1f); 
    }

    public Item getSelected() 
        => selected >= 0 && selected < slots.Length ? slots[selected].getItem() : null;

    public void addItem(Item item)
    {
        foreach (Item i in inInventory)
        {
            if (item.prefab == i.prefab)
            {
                i.usesLeft += item.usesLeft;
                content.updateCount();
                return;
            }
        }
        inInventory.Add(item);
        content.fillSlots();
    }

    private void removeItem(int index)
    {
        inInventory.RemoveAt(index);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].updateIndex(index);
        }
        content.fillSlots();
    }

    public void setItem(int index, int invIndex, Item item) 
        => slots[index].setItem(item, invIndex, spawnPoint);

}

public class Item
{
    public string id;
    public GameObject prefab;
    public Texture texture;
    public int usesLeft;
    public string displayName;

    public Item(GameObject prefab, Texture texture, int uses, string displayName, string id = null)
    {
        this.id = id;
        this.prefab = prefab;
        this.texture = texture;
        usesLeft = uses;
        this.displayName = displayName;
    }
}