using UnityEngine;
using System.Collections;

public class ChestAction : Action {

	[SerializeField]
	private Transform top, open, close;	// Lid, Lid  rotation, Lid close rotation
	[SerializeField]
	private Transform spawnPoint;
    [SerializeField]
    private GameObject hazard;
    [SerializeField]
    private GameObject heal;
	private bool spawned;
	public float speed = 5F;
	public bool canClose;
	
	[HideInInspector]
	public bool isOpen;

	void Update ()
	{
		if(isOpen){
			ChestClicked(open.rotation);
		}
		else{
			ChestClicked(close.rotation);
		}

	}
	
	void ChestClicked(Quaternion toRot){
		if(top.rotation != toRot){
			top.rotation = Quaternion.Lerp(top.rotation, toRot, Time.deltaTime * speed);
		}
	}
	
	public override void action(PlayerInteractor interactor){
		if (!canClose && isOpen) return;
		
		isOpen = canClose ? !isOpen : true;
		if (!spawned && isOpen) spawnItem();
		if (!canClose && isOpen) gameObject.layer = 0;
	}

	public override string setUiPrompt()
	{
		if (!canClose && isOpen) return "";

		return isOpen ? "[E] Close Chest" : "[E] Open Chest";
	}

	void spawnItem()
	{
        GameObject item = Random.value < .5f ? hazard : hazard;
        var itemSpawned = Instantiate(item, spawnPoint.position, Quaternion.identity, spawnPoint);

		var rb = itemSpawned.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.isKinematic = true;
			rb.useGravity = false;
		}
        if (item == hazard)
        {
            itemSpawned.transform.Rotate(10f, 0 ,0);
        }
		spawned = true;
	}
}
