using UnityEngine;
using System.Collections;

public class SettingScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void onClickOK () {
		/*
		UISlider s;
		GameObject cam = GameObject.Find ();
		//
		GameObject sliderDrag = GameObject.Find ("SliderDrag");
		s = sliderDrag.GetComponent<UISlider> ();
		s.sliderValue;
		GameObject sliderForce = GameObject.Find ("SliderForce");
		GameObject sliderForce = GameObject.Find ("SliderLine");
		*/
	}

	public void onClickCancel () {
		Application.LoadLevel ("title");
	}
}
