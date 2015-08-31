using UnityEngine;
using System.Collections;

public class WeaponScript : MonoBehaviour {

	public Transform shotPrefab;

	public void Attack(float speed_x, float speed_y, float dir_x, float dir_y) {
		// Create a new shot
		var shotTransform = Instantiate(shotPrefab) as Transform;
		
		// Assign position
		shotTransform.position = transform.position;

		ShotScript s = shotTransform.gameObject.GetComponent<ShotScript> ();
		HealthScript h = gameObject.GetComponent<HealthScript> ();

		// Make the weapon shot always towards it
		//MoveScript move = shotTransform.gameObject.GetComponent<MoveScript>();
		Rigidbody2D rb = shotTransform.gameObject.GetComponent<Rigidbody2D> ();

		rb.AddForce (new Vector2(speed_x * 10.0f, speed_y * 10.0f));

		/*
		Debug.Log ("WeaponScript: move " + move);
		if (move != null)
		{
			move.DoIt(speed_x, speed_y, dir_x, dir_y);
			//move.direction = this.transform.right; // towards in 2D space is the right of the sprite
		}
		*/
	}
}
