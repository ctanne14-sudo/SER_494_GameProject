using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Threading;
public class InvSlot : MonoBehaviour
{
    [SerializeField] private RawImage icon;
    [SerializeField] private Image highlight;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI usesLeft;
    private Inventory inventory;
    private Item item;
    private bool selected = false;

    private int invIndex = -1;


    void Awake()
    {
        setName();
        setCount();
    }

    public void addInventory(Inventory inventory)
    {
        this.inventory = inventory;
        Button b = GetComponent<Button>();
        b.onClick.RemoveAllListeners();
        b.onClick.AddListener(onClick);
    }

    public void onClick()
    {
        if (item == null) return;
        bool temp = selected;
        inventory.clearSelection();
        selected = !temp;
        highlight.gameObject.SetActive(selected);
        if (selected)
            inventory.setSlotItem(item);
    }

    public void clearSelection()
    {
        selected = false;
        highlight.gameObject.SetActive(selected);        
    }

    public void setItem(Item item, int index)
    {
        this.item = item;
        icon.texture = item.texture;
        icon.enabled = item.texture != null;
        invIndex = index;
        usesLeft.text = item.usesLeft.ToString();
        setName();
    }

    public int clear()
    {
        int index = invIndex;
        item = null;
        icon.texture = null;
        icon.enabled = false;
        invIndex = -1;
        setName();
        return index;
    }

    public void updateIndex(int index)
    {
        if (invIndex > index) invIndex--;
    }

    public void setName() 
    {
        itemName.text = item != null ? item.displayName : "";
    }
    
    
    public void setCount()
    {
        usesLeft.text = item != null ? item.usesLeft.ToString() : "";
    }
}
