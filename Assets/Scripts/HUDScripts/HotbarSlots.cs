using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] private RawImage icon;
    [SerializeField] private Image highlight;
    [SerializeField] private Item item;
    [SerializeField] private TextMeshProUGUI slotNum;
    [SerializeField] private TextMeshProUGUI itemName;
    private GameObject spawnPoint;
    private GameObject spawned;
    private bool isSelected;

    void Start()
    {
        setName();
    }

    public Item setItem(Item item, GameObject spawnPoint)
    {
        Item temp = this.item;
        clear();
        this.spawnPoint = spawnPoint;
        this.item = item;
        icon.texture = item.texture;
        icon.enabled = item.texture != null;
        spawnItem();
        setName();
        return temp;
    }

    public void setSelected(bool selected)
    {
        isSelected = selected;
        highlight.gameObject.SetActive(isSelected);
        if (selected) transform.localScale = new Vector2(1.05f, 1.05f);
        else transform.localScale = new Vector2(1f, 1f);
        spawnItem();
    }

    private void spawnItem()
    {
        if (isSelected && item != null && spawned == null) {
            Transform sp = spawnPoint.transform;
            spawned = Instantiate(item.prefab, sp.position, sp.rotation, sp);

            Transform offset = spawned.transform.Find("handOffset");
            if (offset != null)
            {
                Quaternion rel = Quaternion.Inverse(spawned.transform.rotation) * offset.rotation;
                spawned.transform.rotation = sp.rotation * Quaternion.Inverse(rel);

                spawned.transform.position += sp.position - offset.position;
            }
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
        setName();
        if (spawned != null) Destroy(spawned);
    }

    public Item getItem() => item;

    public void setNum(int num) => slotNum.text = num.ToString();

    public void setName() =>
        itemName.text = item != null ? item.displayName : "";
}