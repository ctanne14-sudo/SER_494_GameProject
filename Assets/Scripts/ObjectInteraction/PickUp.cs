using UnityEngine;
using TMPro;

public abstract class PickUp : Action

{
    [Header("UI")]
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, .5f, 0);
    [SerializeField] private float uiScale = .01f;
    [SerializeField] protected string displayName;
    private GameObject spawnedUI;
    private TextMeshProUGUI uiTextComponent;

    void Start()
    {
        if (uiPrefab != null)
        {
            spawnedUI = Instantiate(uiPrefab, transform.position + uiOffset, Quaternion.identity, transform);
            spawnedUI.transform.localScale = Vector3.one * uiScale;
            uiTextComponent = spawnedUI.GetComponentInChildren<TextMeshProUGUI>();
            uiTextComponent.text = "[E] Pick Up " + displayName;
            spawnedUI.SetActive(false);
        }
    }


    public override void action(PlayerInteractor interactor)
    {
        pickUp(interactor);        
        Destroy(gameObject);
    }

    protected abstract void pickUp(PlayerInteractor interactor);

    public override void showUI(bool show)
    {
        if (spawnedUI != null) spawnedUI.SetActive(show);
    }
}