using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] RawImage icon;
    [SerializeField] GameObject highlight;
    [SerializeField] GameObject item;

    public void SetItem(GameObject item, Texture itemImage)
    {
        if (icon == null)
    {
        Debug.LogError($"HotbarSlot '{name}' has no icon assigned", this);
        return;
    }
        this.item = item;
        icon.texture = itemImage;
        icon.enabled = itemImage != null;
    }

    public void SetSelected(bool selected) => highlight.SetActive(selected);

    public GameObject getItem()
    {
        return item;
    }
}