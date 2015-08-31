using UnityEngine;
using System.Collections;

public class ShotScript : MonoBehaviour {
	public int damage = 1;
	public int side = 0;
	
	/// <summary>
	/// Projectile damage player or enemies?
	/// </summary>
	public bool isEnemyShot = false;
	
	// Use this for initialization
	void Start () {
		// 2 - Limited time to live to avoid any leak
		Destroy(gameObject, 20); // 20sec
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
