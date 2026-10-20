using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] private RawImage icon;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject item;
    private GameObject spawnPoint;
    private GameObject spawned;
    private int invIndex = -1;
    private bool isSelected;



    public void SetItem(GameObject item, Texture itemImage, int index, GameObject spawnPoint)
    {
        this.spawnPoint = spawnPoint;
        this.item = item;
        icon.texture = itemImage;
        icon.enabled = itemImage != null;
        invIndex = index;
        spawnItem();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        highlight.SetActive(isSelected);
        spawnItem();
    }

    private void spawnItem()
    {
        if (isSelected && item != null && spawned == null) {
            spawned = Instantiate(item, spawnPoint.transform.position, Quaternion.identity, spawnPoint.transform);
        }
        if (spawned != null && !isSelected)
        {
            Destroy(spawned);
        }
    }

    public void updateIndex(int index)
    {
        if (invIndex > index)
        {
            invIndex -= 1;
        }
    }

    public int clear()
    {
        int index = invIndex;
        item = null;
        icon.texture = null;
        icon.enabled = false;
        invIndex = -1;
        if (spawned != null) Destroy(spawned);
        return index;
    }
        

    public GameObject getItem()
    {
        return item;
    }

    public int getInvIndex()
    {
        return invIndex;
    }
}