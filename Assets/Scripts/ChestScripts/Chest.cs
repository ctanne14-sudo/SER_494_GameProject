using UnityEngine;
using TMPro;

public class Chest : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string prompt = "[E] Open Chest";
    [SerializeField] private GameObject uiPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 1.5f, 0); // Floats 1.5 meters above the object

    private GameObject _spawnedUI;
    private TextMeshProUGUI _uiTextComponent;

    public string InteractionPrompt => prompt;

    private void Start()
    {
        // 1. Spawn the floating UI text above this specific object
        if (uiPrefab != null)
        {
            _spawnedUI = Instantiate(uiPrefab, transform.position + uiOffset, Quaternion.identity, transform);
            _uiTextComponent = _spawnedUI.GetComponentInChildren<TextMeshProUGUI>();
            
            if (_uiTextComponent != null)
            {
                _uiTextComponent.text = prompt;
            }
            
            // Hide it initially
            _spawnedUI.SetActive(false);
        }
    }

    public void ShowUI(bool show)
    {
        if (_spawnedUI != null)
        {
            _spawnedUI.SetActive(show);
        }
    }

    public bool Interact(PlayerInteractor interactor)
    {
        

        var openChest = GetComponent<OpenChest>();
        if (openChest != null)
        {
            bool isOpen = openChest.OpenCloseChest();
            bool canClose = openChest.canClose;

            if (isOpen && canClose)
            {
                _uiTextComponent.text = "[E] Close Chest";
            }
            else if (!isOpen && canClose)
            {
                _uiTextComponent.text = "[E] Open Chest";
            }
            else
            {
                _uiTextComponent.text = "";
                
            }
        }

        return true;
    }
}
