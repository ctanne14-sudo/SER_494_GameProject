using UnityEngine;
using TMPro;

public class PickUp : Action
{
    [Header("Object")]
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Texture itemImage;
    [SerializeField] private int uses;

    [Header("UI")]
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, .5f, 0);
    [SerializeField] private float uiScale = .01f;
    private GameObject spawnedUI;
    private TextMeshProUGUI uiTextComponent;

    private void Start()
    {
        Vector3 scale = Vector3.one * uiScale;
        if (uiPrefab != null)
        {
            spawnedUI = Instantiate(uiPrefab, transform.position + uiOffset, Quaternion.identity, transform);
            spawnedUI.transform.localScale = scale;
            uiTextComponent = spawnedUI.GetComponentInChildren<TextMeshProUGUI>();
            uiTextComponent.text = setUiPrompt();
            spawnedUI.SetActive(false);
        }
    }


    public override void action(PlayerInteractor interactor)
    {
        if (gameObject.CompareTag("InventoryItem")) {
            interactor.inventory.addItem(new Item(itemPrefab, itemImage, uses));
        } else
        {
            Action itemAction = itemPrefab.GetComponent<Action>();
            itemAction.action(interactor);
        }
        Destroy(gameObject);
    }

    public override string setUiPrompt()
    {
        Action itemAction = itemPrefab.GetComponent<Action>();
        string itemPrompt = itemAction.setUiPrompt();

        return "[E] Pick Up " + itemPrompt;
    }
    public override void showUI(bool show)
    {
        if (spawnedUI != null) spawnedUI.SetActive(show);
    }
}