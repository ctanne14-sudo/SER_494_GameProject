using UnityEngine;
using System.Collections;

public class ChestAction : Action {

	[SerializeField] private Transform top, open, close;
	[SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject[] items;
	[SerializeField] private float speed = 5F;
    [SerializeField] private bool canClose;
	private GameObject[] itemSpawned;
	private bool spawned;
	private bool isOpen;
	
	void Start()
	{
		itemSpawned = new GameObject[spawnPoints.Length + 1];
	}

	void Update ()
	{
		Quaternion toRot = isOpen ? open.rotation : close.rotation;
		if(top.rotation != toRot){
			top.rotation = Quaternion.Lerp(top.rotation, toRot, Time.deltaTime * speed);
		}
	}
	
	public override void action(PlayerInteractor interactor){
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

	public override string setUiPrompt()
	{
		if (!canClose && isOpen) return "";
		return isOpen ? "[E] Close Chest" : "[E] Open Chest";
	}

	void spawnItem()
	{
		if (spawnPoints.Length > 0 && items.Length > 0) {
			int numSpawn = Random.Range(1, spawnPoints.Length + 1);
			for (int i = 0; i < numSpawn; i++) {
				GameObject item = items[Random.Range(0, items.Length)];
				itemSpawned[i] = Instantiate(item, spawnPoints[i].position, Quaternion.identity, spawnPoints[i]);
				spawned = true;
			}
		}
	}
}
