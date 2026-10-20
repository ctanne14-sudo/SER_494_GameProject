using UnityEngine;
using System.Collections;
using TMPro;

public class ChestAction : Action {

	[Header("Chest Objects")]
	[SerializeField] private Transform top;
	[SerializeField] private Transform open;
	[SerializeField] private Transform close;
	[SerializeField] private Transform[] spawnPoints;

	[Header("Chest Items")]
    [SerializeField] private GameObject[] items;
    [SerializeField] private GameObject unlockItem;

	[Header("Chest Attributes")]
	[SerializeField] private float speed = 5F;
    [SerializeField] private bool canClose;
    [SerializeField] private bool unlock;

	private GameObject[] itemSpawned;
	private bool spawned;
	private bool isOpen;
	
	[Header("Chest UI")]
	[SerializeField] private GameObject uiPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 1.5f, 0);
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
		itemSpawned = new GameObject[spawnPoints.Length + 1];
    }


	void Update ()
	{
		Quaternion toRot = isOpen ? open.rotation : close.rotation;
		if(top.rotation != toRot){
			top.rotation = Quaternion.Lerp(top.rotation, toRot, Time.deltaTime * speed);
		}
	}
	
	void spawnItem()
	{
		if (spawnPoints.Length > 0 && items.Length > 0) {
			int numSpawn = Random.Range(1, spawnPoints.Length + 1);
			for (int i = 0; i < numSpawn; i++) {
				GameObject item = items[Random.Range(0, items.Length)];
				itemSpawned[i] = Instantiate(item, spawnPoints[i].position, Quaternion.identity, spawnPoints[i]);
				itemSpawned[i].transform.SetParent(null);
				itemSpawned[i].transform.Rotate(0f, 0f, 0f);
				spawned = true;
			}
		}
	}
	public override void action(PlayerInteractor interactor){
		if (!unlock) {
			if (!canClose && isOpen) return;
			
			isOpen = canClose ? !isOpen : true;
			if (!spawned && isOpen) spawnItem();
			if (spawned && !isOpen) {
				for (int i = 0; i < itemSpawned.Length; i++)
					Destroy(itemSpawned[i]);
				spawned = false;
			}
			if (!canClose && isOpen) gameObject.layer = 0;
		}
	}

	public override string setUiPrompt()
	{
		if (!canClose && isOpen) return "";
		else if (!unlock) 
			return isOpen ? "[E] Close Chest" : "[E] Open Chest";
		else return "KEY Required to Open";
	}

    public override void showUI(bool show)
    {
        if (spawnedUI != null) spawnedUI.SetActive(show);
    }
}
