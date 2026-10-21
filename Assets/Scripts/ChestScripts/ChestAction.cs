using UnityEngine;
using TMPro;

public class ChestAction : Action {

	[Header("Chest Objects")]
	[SerializeField] private string displayName;
	[SerializeField] private Transform top;
	[SerializeField] private Transform open;
	[SerializeField] private Transform close;
	[SerializeField] private Transform[] spawnPoints;

	[Header("Chest Items")]
    [SerializeField] private GameObject[] items;

	[Header("Chest Attributes")]
	[SerializeField] private float speed = 5F;
    [SerializeField] private bool canClose;
	[SerializeField] private string lockId;
	[SerializeField] private bool setLock;
	[SerializeField] private Inventory inventory;

    private bool unlocked;
	private bool hasKey;

	private GameObject[] itemSpawned;
	private bool spawned;
	private bool isOpen;
	
	[Header("Chest UI")]
	[SerializeField] private GameObject uiPrefab;
    [SerializeField] private Vector3 uiOffset = new Vector3(0, 1.5f, 0);
    [SerializeField] private float uiScale = .01f;
	private UIShake uiShake;
    private GameObject spawnedUI;
    private TextMeshProUGUI uiTextComponent;

    private void Start()
    {
		unlocked = !setLock || isOpen;
        if (uiPrefab != null)
        {
            spawnedUI = Instantiate(uiPrefab, transform.position + uiOffset, Quaternion.identity, transform);
            spawnedUI.transform.localScale = Vector3.one * uiScale;
            uiTextComponent = spawnedUI.GetComponentInChildren<TextMeshProUGUI>();
            spawnedUI.SetActive(false);
			uiTextComponent.text = setUiPrompt();
			uiShake = spawnedUI.GetComponentInChildren<UIShake>();
			uiShake.enabled = false;
        }
		itemSpawned = new GameObject[spawnPoints.Length + 1];
    }


	void Update ()
	{
		Quaternion toRot = isOpen ? open.rotation : close.rotation;
		if(top.rotation != toRot){
			top.rotation = Quaternion.Lerp(top.rotation, toRot, Time.deltaTime * speed);
		}

		if (setLock && !unlocked)
			checkForKey();
	}

	private void checkForKey()
	{
		Item item = inventory.getSelected();
		bool stateChange = item != null && item.id == lockId;

		if (stateChange == hasKey) return;

		hasKey = stateChange;
		uiTextComponent.text = setUiPrompt();
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
		if (!canClose && isOpen) return;
        		
		if (unlocked)
		{
			isOpen = canClose ? !isOpen : true;
			if (!spawned && isOpen) spawnItem();
			else if (spawned && !isOpen) {
				foreach (var item in itemSpawned)
					Destroy(item);
				spawned = false;
			}

			if (!canClose && isOpen) gameObject.layer = 0;
			unlocked = !setLock || isOpen;
		} else
		{
			Item item = inventory.getSelected();
			if (item != null && item.id == lockId)
			{
				item.usesLeft--;
				unlocked = true;
			} else {
					uiShake.shake();
			}
		}
		uiTextComponent.text = setUiPrompt();
	}

	public string setUiPrompt()
	{
		if (isOpen) return canClose ? "[E] Close " + displayName : "";
		if (unlocked) return "[E] Open " + displayName;
		if (hasKey) return "[E] Unlock " + displayName;
		return lockId + " Required to Open";
	}

    public override void showUI(bool show)
    {
        if (spawnedUI != null) spawnedUI.SetActive(show);
    }
}
