using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderLine : MonoBehaviour {
	private float sliderValue;
	
	void Start() {
		sliderValue = (preferences.drag - preferences.drag_Min) / (preferences.drag_Max - preferences.drag_Min);
		gameObject.GetComponent<Slider> ().value = sliderValue;
		//onSliderValueChanged ();
	}
	
	public void onSliderValueChanged() {
		
		sliderValue = gameObject.GetComponent<Slider> ().value;
		preferences.arrowMaxLen = preferences.arrowMaxLen_Min + sliderValue * (preferences.arrowMaxLen_Max - preferences.arrowMaxLen_Min);
		
		// 在标签上显示当前数值
		GameObject.Find ("TextLine").GetComponent<Text> ().text = preferences.arrowMaxLen.ToString ();
	}
}
