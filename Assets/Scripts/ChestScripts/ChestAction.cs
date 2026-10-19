using UnityEngine;
using System.Collections;

public class ChestAction : WorldAction {

	[SerializeField]
	private Transform top, open, close;	// Lid, Lid  rotation, Lid close rotation
	[SerializeField]
	private Transform spawnPoint;
    [SerializeField]
    private GameObject hazard;
	[SerializeField]
    private GameObject heal;
    [SerializeField]
	private float speed = 5F;
    [SerializeField]
	private bool canClose;
	private GameObject itemSpawned;
	private bool spawned;
	private bool isOpen;
	

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
			Destroy(itemSpawned);
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
        GameObject item = Random.value < .5f ? heal : hazard;
        itemSpawned = Instantiate(item, spawnPoint.position, Quaternion.identity, spawnPoint);
        if (item == hazard)
        {
            itemSpawned.transform.Rotate(10f, 0 ,0);
        }
		spawned = true;
	}
}
