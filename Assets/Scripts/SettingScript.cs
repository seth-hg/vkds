using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingScript : MonoBehaviour {
	
	void Start () {
		//onSliderValueChanged ();
	}

	public void onClickOK () {
		Application.LoadLevel ("title");
	}

	public void onClickCancel () {
		Application.LoadLevel ("title");
	}
}
