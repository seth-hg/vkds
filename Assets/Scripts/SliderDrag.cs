using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderDrag : MonoBehaviour {
	private float sliderValue;

	void Start() {
		sliderValue = (preferences.drag - preferences.drag_Min) / (preferences.drag_Max - preferences.drag_Min);
		gameObject.GetComponent<Slider> ().value = sliderValue;
		//onSliderValueChanged ();
	}

	public void onSliderValueChanged() {

		sliderValue = gameObject.GetComponent<Slider> ().value;
		preferences.drag = preferences.drag_Min + sliderValue * (preferences.drag_Max - preferences.drag_Min);

		// 在标签上显示当前数值
		GameObject.Find ("TextDrag").GetComponent<Text> ().text = preferences.drag.ToString ();
	}
}
