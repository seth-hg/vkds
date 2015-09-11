using UnityEngine;
using System.Collections;

public class TouchMenu : MonoBehaviour {

    public GameObject dialog = null;

	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if ( Input.GetMouseButtonDown(0))
        {
            dialog.SetActive(true);
        }
    }
}
