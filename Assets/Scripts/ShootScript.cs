using UnityEngine;
using System.Collections;

public class ShootScript : MonoBehaviour {
	private bool mouse_clicked = false;
	//private System.DateTime ts_mouse_start = new System.DateTime();
	
	public Material mat;
	public Color color = Color.red;
	public Vector2 pos1;
	public Vector2 pos2;
	public bool isReady = false;
	private RaycastHit2D hit;
	
	private Vector3 startVertex;
	private Vector3 mousePos;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		mousePos = Input.mousePosition;
		if (Input.GetMouseButtonDown (0)) {
			hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
			if (hit) {
				Debug.Log("hit");
				//ts_mouse_start = System.DateTime.Now;
				mouse_clicked = true;
				
				//ball = GameObject.Find (hit.transform.name);
				pos1 = Input.mousePosition;
				startVertex = new Vector3(pos1.x / Screen.width, pos1.y / Screen.height, 0);
				//startVertex = new Vector3(hit.transform.position.x, hit.transform.position.y, 0);
			}
		}
		if (mouse_clicked && Input.GetMouseButtonUp (0)) {
			mouse_clicked = false;
			pos2 = Input.mousePosition;
			Vector2 dir = pos2 - pos1;
			//dir.x = Mathf.Abs (dir.x);
			//dir.y = Mathf.Abs (dir.y);

			float dir_x = 0.0f;
			float dir_y = 0.0f;
			if (pos2.x > pos1.x) dir_x = 1;
			else if (pos2.x < pos1.x) dir_x = -1;
			
			if (pos2.y > pos1.y) dir_y = 1;
			else if (pos2.y < pos1.y) dir_y = -1;
			
			isReady = true;
			
			//Debug.Log ("Moved: " + speed_factor + dir_x + dir_y);
			
			//MoveScript m = ball.GetComponent<MoveScript>();
			//m.DoIt(dir.x, dir.y, dir_x, dir_y);

			// call WeaponScript to shoot
			GameObject obj = GameObject.Find (hit.transform.name);
			Debug.Log ("name " + hit.transform.name);
			Debug.Log ("obj " + obj);
			WeaponScript weapon = obj.GetComponent<WeaponScript>();
			Debug.Log ("weapon " + weapon);
			if (weapon != null)
			{
				// false because the player is not an enemy
				weapon.Attack(dir.x, dir.y, dir_x, dir_y);
			}
		}
	}

	void OnPostRender() {
		if (!mouse_clicked)
			return;
		if (!mat) {
			Debug.LogError("Please Assign a material on the inspector");
			return;
		}
		GL.PushMatrix();
		mat.SetPass(0);
		GL.LoadOrtho();
		GL.Begin(GL.LINES);
		GL.Color(Color.yellow);
		GL.Vertex(startVertex);
		GL.Vertex(new Vector3(mousePos.x / Screen.width, mousePos.y / Screen.height, 0));
		GL.End();
		GL.PopMatrix();
		
		//Debug.Log ("in OnPostRender()" + hit.transform.localPosition + startVertex);

	}

	void OnGUI() {
		if(Input.GetKeyDown(KeyCode.Escape)) {
			Application.LoadLevel("title");
		}
	}
}
