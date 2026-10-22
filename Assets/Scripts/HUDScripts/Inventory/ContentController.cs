using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ContentController : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private GameObject verticalLayout;

    public List<InventoryRow> inventoryRows = new List<InventoryRow>();


    void Awake()
    {
        foreach (InventoryRow row in inventoryRows)
            foreach (InvSlot slot in row.slots)
                slot.addInventory(inventory);
    }

    void addRow()
    {
            GameObject spawn = Instantiate(rowPrefab, verticalLayout.transform, false);
            InventoryRow row = spawn.GetComponent<InventoryRow>();
            foreach (InvSlot slot in row.slots) 
                slot.addInventory(inventory);
            inventoryRows.Add(row);
    }

    public void fillSlots()
    {
        int items = 0;
        int rows = 0;
        while (true)
        {
            while (items < inventory.inInventory.Count && inventory.isInSlot(items)) items++;
            if (items >= inventory.inInventory.Count) break;

            if (rows >= inventoryRows.Count - 1) addRow();


            foreach (InvSlot slot in inventoryRows[rows].slots)
            {
                if (items < inventory.inInventory.Count) {
                    slot.setItem(inventory.inInventory[items], items);
                    items++;
                } else slot.clear();
            }
            rows++;
        }

        for (int i = inventoryRows.Count - 1; i >= rows + 1; i--)
        {
            Destroy(inventoryRows[rows].gameObject);
            inventoryRows.RemoveAt(rows);
        }
    }

    public void clearSelection()
    {
        foreach (InventoryRow row in inventoryRows)
            foreach (InvSlot slot in row.slots) 
                slot.clearSelection();
        
    }

    public void updateCount()
    {
        foreach (InventoryRow row in inventoryRows)
            foreach (InvSlot slot in row.slots)
                slot.setCount();
    }

    public void showInventory(bool show)
    {
        transform.root.gameObject.SetActive(show);
    }
}
