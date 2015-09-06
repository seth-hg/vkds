using UnityEngine;
using System.Collections;

public class MainMenu : MonoBehaviour {

	public void onClickSingle () {
		Application.LoadLevel ("battle");
	}

	public void onClickLobby () {
		Application.LoadLevel ("battleNetwork");
	}

	public void onClickSettings () {
		Application.LoadLevel ("settings");
	}

	public void onClockExit () {
		Application.Quit ();
	}

	void OnGUI()
	{
		if (Input.GetKeyDown (KeyCode.Escape)) {
			Application.Quit ();
		}
	}
}
