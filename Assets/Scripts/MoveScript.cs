using UnityEngine;
using System.Collections;

public class MoveScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

	void OnTriggerEnter2D(Collider2D otherCollider)
	{
		// 根据otherCollider的类型来判断应有的反应
		//GetComponent<Rigidbody2D> ().velocity = new Vector2 (0, 0);
	}
}
