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
            for (int s = 0; s < slots.Length; s++) {
                if (slots[s].getItem() != null) continue;

                int i = 0;
                while (i < inInventory.Count && isInSlot(i)) i++;
                if (i >= inInventory.Count) break;

                setItem(s, i, inInventory[i], textures[i]);
            }
        }

        if (Input.GetMouseButtonUp(0) && selected >= 0 && selected < slots.Length) 
        {
            GameObject item = slots[selected].getItem();
            if (item != null) {
                item.GetComponent<ObjectInteraction>().action(interactor, 0);
                int index = slots[selected].clear();
                if (index >= 0 && index < inInventory.Count) removeItem(index);

                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i].updateIndex(index);
                }
            }
        }

    }

    bool isInSlot(int invIndex)
    {
        for (int i = 0; i < slots.Length; i++) 
            if (invIndex == slots[i].getInvIndex()) return true;
        return false;
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

    public void setItem(int index, int invIndex, GameObject item, Texture itemImage) 
        => slots[index].SetItem(item, itemImage, invIndex, spawnPoint);

}