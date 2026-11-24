using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarSlot : MonoBehaviour
{
    [SerializeField] private RawImage icon;
    [SerializeField] private Image highlight;
    [SerializeField] private TextMeshProUGUI slotNum;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private ItemDatabase db;
    private string itemID = null;
    private GameObject spawnPoint;
    private GameObject spawned;
    private bool isSelected;

    void Start()
    {
        setName();
    }

    public string setItemID(string itemID, GameObject spawnPoint)
    {
        string temp = this.itemID;
        clear();
        ItemData data = string.IsNullOrEmpty(itemID) ? null : db.Get(itemID);
        icon.texture = data != null ? data.icon : null;
        icon.enabled = data != null;
        this.spawnPoint = spawnPoint;
        this.itemID = itemID;
        spawnItem();
        setName();
        return temp;
    }

    public void setSelected(bool selected)
    {
        isSelected = selected;
        highlight.gameObject.SetActive(selected);
        if (selected) transform.localScale = new Vector2(1.025f, 1.025f);
        else transform.localScale = new Vector2(1f, 1f);
        spawnItem();
    }

    private void spawnItem()
    {
        if (isSelected && itemID != null && spawned == null) {
            ItemData itemData = db.Get(itemID);
            if (string.IsNullOrEmpty(itemID) || itemData.prefab == null) return;

            Transform sp = spawnPoint.transform;
            spawned = Instantiate(itemData.prefab, sp.position, sp.rotation, sp);

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
        itemID = null;
        icon.texture = null;
        icon.enabled = false;
        setName();
        if (spawned != null) Destroy(spawned);
    }

    public string getItemID() => itemID;

    public void setNum(int num) => slotNum.text = num.ToString();

    public void setName() {
        ItemData data = string.IsNullOrEmpty(itemID) ? null : db.Get(itemID);
        itemName.text = data != null ? data.displayName : itemID;
    }
}