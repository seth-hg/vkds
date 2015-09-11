using UnityEngine;
using System.Collections;

public class GameMenu : MonoBehaviour {

    private float scale = 0;

    void Start()
    {
        gameObject.SetActive(false);
        transform.localScale = new Vector3(scale, scale, scale);
    }

    void Update()
    {
        if (scale <= 1)
            scale += 0.1f;
        transform.localScale = new Vector3(scale, scale, scale);
    }

    void OnGUI() {
        gameObject.SetActive(true);
    }

    public void onClickMenu()
    {
        gameObject.SetActive(true);
    }

    public void onClickExit()
    {
		// 在网络模式下需发送CLOSE通知服务器
		ClientScript cs = Camera.main.GetComponent<ClientScript> ();
		//Debug.Log ("cs = " + cs);
		if (cs != null)
			cs.sendClose ();
		Application.LoadLevel("lose");
    }

    public void onClickButton()
    {
        gameObject.SetActive(false);
    }
}
