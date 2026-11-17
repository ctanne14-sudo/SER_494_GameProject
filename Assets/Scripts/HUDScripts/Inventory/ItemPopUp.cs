using UnityEngine;
using TMPro;

public class ItemPopUp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usesLeft;
    [SerializeField] private TextMeshProUGUI displayName;

    private Item item;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
        }
        if (item != null)
        {
            usesLeft.text = "x" + item.usesLeft;
            displayName.text = item.displayName;
        } else
        {
            usesLeft.text = "x0";
            displayName.text = "";
        }
    }

    void OnDisable()
    {
        gameObject.SetActive(false);       
    }

    public void setItem(Item item) => this.item = item;
}
