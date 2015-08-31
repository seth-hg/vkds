using UnityEngine;
using System.Collections;

//[AddComponentMenu("MyGame/TitleScreen")]
public class TitleScreen : MonoBehaviour
{
	void Start ()
	{
		Screen.SetResolution (480, 640, false);
	}

	void OnGUI()
	{
		if(Input.GetKeyDown(KeyCode.Escape)) {
			Application.LoadLevel("title");
		}

		// 文字大小
		GUI.skin.label.fontSize = 48;
		
		// UI中心对齐
		GUI.skin.label.alignment = TextAnchor.LowerCenter;
		
		// 显示标题
		GUI.Label(new Rect(0, 40, Screen.width, 120), "标题党");
		
		// 开始游戏按钮
		if (GUI.Button(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 0, 200, 30), "人机对战"))
		{
			// 开始读取下一关
			Application.LoadLevel("battle");
		}

		if (GUI.Button(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 50, 200, 30), "游戏大厅"))
		{
			// 开始读取下一关
			Application.LoadLevel("lobby");
		}

		if (GUI.Button(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 100, 200, 30), "设置参数"))
		{
			// 开始读取下一关
			Application.LoadLevel("settings");
		}

		if (GUI.Button(new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f + 150, 200, 30), "退出游戏"))
		{
			// 开始读取下一关
			Application.Quit ();
		}
	}
}