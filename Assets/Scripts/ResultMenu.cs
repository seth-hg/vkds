using UnityEngine;
using System.Collections;

public class ResultMenu : MonoBehaviour
{
	public bool isNetwork = false;

    private float scale = 0;

    void Start()
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    void Update()
    {
        if (scale <= 1)
            scale += 0.1f;
        transform.localScale = new Vector3(scale, scale, scale);
    }

	public void onClickOK()
	{
		if (isNetwork)
			Application.LoadLevel("battleNetwork");
		else
			Application.LoadLevel("battle");
	}

    public void onClickExit()
    {
        Application.LoadLevel("title");
    }
}
