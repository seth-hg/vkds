using UnityEngine;
using System.Collections;

public class WaitingScript : MonoBehaviour {

	//private float scale = 0;
	
	void Start()
	{
		gameObject.SetActive(true);
		//transform.localScale = new Vector3(scale, scale, scale);
	}

	/*
	void Update()
	{
		if (scale <= 1)
			scale += 0.1f;
		transform.localScale = new Vector3(scale, scale, scale);
	}
	*/

	public void hideDialog() {
		gameObject.SetActive(false);
	}

	/*
	void OnGUI() {
		gameObject.SetActive(true);
	}
	*/
	
	public void onClickCancel()
	{
		ClientScript cs = Camera.main.GetComponent<ClientScript> ();
		Debug.Assert (cs != null);
		cs.sendCancel ();
		Application.LoadLevel("title");
	}
}
