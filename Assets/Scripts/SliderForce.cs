using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderForce : MonoBehaviour {
	private float sliderValue;
	
	void Start() {
		sliderValue = (preferences.drag - preferences.drag_Min) / (preferences.drag_Max - preferences.drag_Min);
		gameObject.GetComponent<Slider> ().value = sliderValue;
		//onSliderValueChanged ();
	}
	
	public void onSliderValueChanged() {
		
		sliderValue = gameObject.GetComponent<Slider> ().value;
		preferences.forceMax = preferences.forceMax_Min + sliderValue * (preferences.forceMax_Max - preferences.forceMax_Min);
		
		// 在标签上显示当前数值
		GameObject.Find ("TextForce").GetComponent<Text> ().text = preferences.forceMax.ToString ();
	}
}
