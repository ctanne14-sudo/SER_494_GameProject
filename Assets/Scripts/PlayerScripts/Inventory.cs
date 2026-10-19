using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [SerializeField] private HotbarSlot[] slots;
    [SerializeField] private PlayerInteractor interactor;

    [SerializeField] private PlayerSpeed ps;
    [SerializeField] private GameObject spawnPoint;

    private List<GameObject> inInventory = new List<GameObject>();
    private List<Texture> textures = new List<Texture>();

    [HideInInspector] public int gold;

    private int selected;

    private readonly KeyCode[] numKeys = {KeyCode.Alpha1, KeyCode.Alpha2, 
        KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5};

    void Update()
    {
        for (int i = 0; i < numKeys.Length; i++)
        {
            if (Input.GetKeyDown(numKeys[i])) Select(i);
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) Select(-1);

        if (inInventory.Count > 0)
        {
            for (int i = 0; i < slots.Length && i < inInventory.Count; i++)
                if (slots[i].getItem() == null)
                    SetItem(i, inInventory[i], textures[i]);
        }

        if (Input.GetMouseButtonUp(0) && selected >= 0 && selected < slots.Length) 
        {
            GameObject item = slots[selected].getItem();
            if (item != null) {
                item.GetComponent<ObjectInteraction>().action(interactor, 0);
                int index = inInventory.IndexOf(item);
                if (index >= 0) removeItem(index);
                slots[selected].clear();
            }
        }

    }

    public void Select(int index)
    {
        if (selected == index) index = -1;
        selected = index;
        bool select = selected >= 0 && selected < slots.Length;
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSelected(select && i == selected);
        ps.changeMultiplier(select ? .8f : 1f); 
    }

    public void addItem(GameObject item, Texture itemImage)
    {
        inInventory.Add(item);
        textures.Add(itemImage);
    }

    private void removeItem(int index)
    {
        inInventory.RemoveAt(index);
        textures.RemoveAt(index);
    }

    public void SetItem(int index, GameObject item, Texture itemImage) => slots[index].SetItem(item, itemImage, spawnPoint);

}