using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [SerializeField] private HotbarSlot[] slots;
    [SerializeField] private PlayerInteractor interactor;

    [SerializeField] private PlayerSpeed ps;
    [SerializeField] private GameObject spawnPoint;

    private List<Item> inInventory = new List<Item>();

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

        fillSlots();

        if (selected >= 0 && selected < slots.Length)
            foreach(KeyCode i in inputKeys)
                if (Input.GetKeyUp(i)) useItem(i);
    }

    void useItem(KeyCode button)
    {
        Item item = slots[selected].getItem();
        if (item != null) {
            if (button == KeyCode.Mouse0) {
                item.prefab.GetComponent<Action>().action(interactor);
                item.usesLeft--;
            }
            if (item.usesLeft <= 0) removeItem(slots[selected].clear());            
        }
    }

    void fillSlots()
    {
        if (inInventory.Count > 0)
        {
            for (int s = 0; s < slots.Length; s++) {
                if (slots[s].getItem() != null) continue;

                int i = 0;
                while (i < inInventory.Count && isInSlot(i)) i++;
                if (i >= inInventory.Count) break;

                setItem(s, i, inInventory[i]);
            }
        }
    }

    bool isInSlot(int invIndex)
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

    public void addItem(Item item)
    {
        inInventory.Add(item);
    }

    private void removeItem(int index)
    {
        inInventory.RemoveAt(index);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].updateIndex(index);
        }
    }

    public void setItem(int index, int invIndex, Item item) 
        => slots[index].setItem(item, invIndex, spawnPoint);

}

public class Item
{
    public GameObject prefab;
    public Texture texture;
    //public string displayName;
    public int usesLeft;

    public Item(GameObject prefab, Texture texture, int uses)
    {
        this.prefab = prefab;
        this.texture = texture;
        usesLeft = uses;
    }
}