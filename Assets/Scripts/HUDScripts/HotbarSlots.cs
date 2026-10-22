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
    private int invIndex = -1;
    private bool isSelected;

    void Start()
    {
        setName();
    }

    public void setItem(Item item, int index, GameObject spawnPoint)
    {
        this.spawnPoint = spawnPoint;
        this.item = item;
        icon.texture = item.texture;
        icon.enabled = item.texture != null;
        invIndex = index;
        spawnItem();
        setName();
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

    public int clear()
    {
        int index = invIndex;
        item = null;
        icon.texture = null;
        icon.enabled = false;
        invIndex = -1;
        setName();
        if (spawned != null) Destroy(spawned);
        return index;
    }
    public void updateIndex(int index)
    {
        if (index == invIndex) clear();
        if (invIndex > index) invIndex--;
    }

    public Item getItem() => item;
    public int getInvIndex() => invIndex;

    public void setNum(int num) => slotNum.text = num.ToString();

    public void setName()
    {
        if (item == null)
        {
            itemName.text = "";
            return;
        }
        itemName.text = item.displayName;
    }
}