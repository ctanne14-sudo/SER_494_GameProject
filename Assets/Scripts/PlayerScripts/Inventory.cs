using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [SerializeField] private HotbarSlot[] slots;
    [SerializeField] private PlayerSpeed ps;
    private List<GameObject> inInventory = new List<GameObject>();
    private List<Texture> textures = new List<Texture>();

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

        if (Input.GetMouseButtonUp(1))
        {
            if (inInventory.Count > 0)
            {
                SetItem(0, inInventory[0], textures[0]);
            }
        }
    }

    public void Select(int index)
    {
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
        Debug.Log("Item added");
    }

    public void SetItem(int index, GameObject item, Texture itemImage) => slots[index].SetItem(item, itemImage);


}