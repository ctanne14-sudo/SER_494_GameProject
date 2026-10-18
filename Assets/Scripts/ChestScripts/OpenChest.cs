using UnityEngine;
using System.Collections;

public class OpenChest : MonoBehaviour {

	public Transform top, open, close;	// Lid, Lid  rotation, Lid close rotation
	public float Speed = 5F;				// Opening speed
	public bool canClose;						// Can the chest be closed
	
	[HideInInspector]
	public bool _open;							// Is the chest opened

	void Update () {
		if(_open){
			ChestClicked(open.rotation);
		}
		else{
			ChestClicked(close.rotation);
		}
	}
	
	// Rotate the top to the requested rotation
	void ChestClicked(Quaternion toRot){
		if(top.rotation != toRot){
			top.rotation = Quaternion.Lerp(top.rotation, toRot, Time.deltaTime * Speed);
		}
	}
	
	public bool OpenCloseChest(){
		if(canClose) _open = !_open; else _open = true;
		return _open;
	}
}
