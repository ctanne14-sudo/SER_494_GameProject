using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] private RawImage icon;
    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject item;
    private GameObject spawnPoint;
    private GameObject spawned;
    private bool isSelected;



    public void SetItem(GameObject item, Texture itemImage, GameObject spawnPoint)
    {
        this.spawnPoint = spawnPoint;
        this.item = item;
        icon.texture = itemImage;
        icon.enabled = itemImage != null;
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

    public void clear()
    {
        item = null;
        icon.texture = null;
        icon.enabled = false;
        if (spawned != null) Destroy(spawned);
    }
        

    public GameObject getItem()
    {
        return item;
    }
}