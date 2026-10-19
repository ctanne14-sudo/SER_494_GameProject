using UnityEngine;
using TMPro;

public class ObjectInteraction : MonoBehaviour
{
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, .5f, 0);
    [SerializeField] private float uiScale = .01f;
    [SerializeField] private Action objectAction;
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
            uiTextComponent.text = objectAction.setUiPrompt();
            spawnedUI.SetActive(false);
        }
    }

    public void ShowUI(bool show)
    {
        if (spawnedUI != null) spawnedUI.SetActive(show);
    }

    public void action(PlayerInteractor interactor, int action)
    {
        objectAction.action(interactor);
        if (action == 1) {
            uiTextComponent.text = objectAction.setUiPrompt();
        }
    }
}
