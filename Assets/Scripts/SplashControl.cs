using UnityEngine;
using System.Collections;

public class SplashControl : MonoBehaviour {

    void Start()
    {
        Invoke("Func", 2f);
    }

    void Func() {
        Application.LoadLevel("title");
    }
}
