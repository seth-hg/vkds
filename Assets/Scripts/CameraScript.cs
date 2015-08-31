using UnityEngine;
using System.Collections;

public class CameraScript : MonoBehaviour {
	
	public Material mat;
	public Color color = Color.red;
	public float arrowMaxLen = 500.0f;	// 击球辅助线最大长度
	public float forceMax = 100.0f;		// 最大击球力度
	private float forceFactor;	// 击球力量系数

	public Transform pieceTrans;		// 用于生成对手小球
	public Transform[] itemPrefabs;		// 道具perfabs

	// 背景图片的尺寸
	public int bgWidth;
	public int bgHeight;

	// 
	private bool isEnemyTurn = false;	// 对手回合
	private bool isPending = false;		// 等待回合结束(所有物体停止运动)

	// selecting ball
	private RaycastHit2D hit;
	private GameObject ball;			// 当前被选中的小球

	public int itemsPerRound = 2;		// 每回合产生的道具数
	public int currentRound = 0;		// 当前回合数

	// drawing arrow & attack
	private bool mouse_clicked = false;
	private Vector2 arrowStart;
	private Vector2 arrowEnd;
	private Vector3 startVertex;

	private int playerScore, enemyScore;
	private bool playerReady = true, enemyReady = true;
	private GameObject settings;
	private bool settingsActive = false;

	private Vector3 savedPos;
	private int frameCounter;

	private float cameraSize;
	private string debugStr = null;

	private GameObject arrowPlayer, arrowEnemy;

	private bool[,] locationMap;// = new bool[9,19]; 

	void Start() {
		cameraSize = bgHeight / 200.0f;
		//cameraSize = 3.2f;
		Camera.main.orthographicSize = cameraSize;
		debugStr = cameraSize.ToString ();
		
		playerScore = 5;
		enemyScore = 5;
		frameCounter = 0;

		arrowMaxLen = Screen.height / 5.0f;
		forceFactor = forceMax / arrowMaxLen;
		Debug.Log ("arrowMaxLen: " + arrowMaxLen);
		Debug.Log ("forceFactor: " + forceFactor);
		int col, row;
		locationMap = new bool[9, 19];
		for (col = 0; col < 9; col ++)
			for (row = 0; row < 19; row ++)
				locationMap [col, row] = false;

		newRound ();

		isEnemyTurn = Random.Range(0, 2) == 0;
		arrowPlayer = GameObject.Find ("arrowPlayer");
		arrowEnemy = GameObject.Find ("arrowEnemy");
		if (isEnemyTurn) {
			arrowPlayer.SetActive (false);
		} else {
			arrowEnemy.SetActive (false);
		}
	}

	// Update is called once per frame
	void Update () {
		// TODO: 尝试每5帧计算一次，提高效率
		/*
		if (!isPending && !isEnemyTurn && Input.GetMouseButtonDown (0)) {
			mouse_clicked = true;
		} else if (mouse_clicked && Input.GetMouseButtonUp (0)) {
			mouse_clicked = false;
		}
		if (frameCounter < 4) {
			frameCounter += 1;
			return;
		}
		frameCounter = 0;
		*/
		arrowEnd = Input.mousePosition;
		Vector2 dir = arrowStart - arrowEnd;
		float angle = Vector2.Angle(dir, new Vector2(1, 0));
		float len = Vector2.Distance(arrowStart, arrowEnd);

		if (len > arrowMaxLen) {
			dir.x = dir.x * arrowMaxLen / len;
			dir.y = dir.y * arrowMaxLen / len;
			arrowEnd = arrowStart + dir;
		}
		arrowEnd = arrowStart + dir;

		//if (!isPending && !isEnemyTurn && Input.GetMouseButtonDown (0)) {
		if (!isPending && Input.GetMouseButtonDown (0)) {
			hit = Physics2D.Raycast (Camera.main.ScreenToWorldPoint (Input.mousePosition), Vector2.zero);
			if (hit) {
				//ts_mouse_start = System.DateTime.Now;
				ball = GameObject.Find (hit.transform.name);
				HealthScript hs = ball.GetComponent<HealthScript> ();
				if (hs.isEnemy != isEnemyTurn) {
					return;
				}

				mouse_clicked = true;
				// 调整main camera
				savedPos = Camera.main.transform.position;
				Camera.main.orthographicSize = cameraSize / 1.5f;
				Camera.main.transform.position = new Vector3(ball.transform.position.x, ball.transform.position.y, -10);

				arrowStart = Camera.main.WorldToScreenPoint(ball.transform.position);
				startVertex = new Vector3 (arrowStart.x / Screen.width, arrowStart.y / Screen.height, 0);
				arrowEnd = arrowStart;
			}
		}

		if (mouse_clicked && !playerReady) {
			//Vector2 p = Input.mousePosition;
			Vector2 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			ball.transform.position = new Vector3(p.x, p.y, -5);
		}

		if (mouse_clicked && Input.GetMouseButtonUp (0)) {
			mouse_clicked = false;
			if (playerReady && enemyReady) { // if battle started

				dir.x = dir.x * forceFactor;
				dir.y = dir.y * forceFactor;

				/*
				Rigidbody2D rb = ball.GetComponent<Rigidbody2D> ();
				rb.AddForce (dir);

				HealthScript hs = ball.GetComponent<HealthScript> ();
				hs.isReady = false;
				hs.attacking = true;
				*/
				Camera.main.transform.position = savedPos;
				Camera.main.orthographicSize = cameraSize;
				ball.GetComponent<HealthScript> ().Attack(dir);
				isPending = true;
				Debug.Log ("velocity: " + ball.GetComponent<Rigidbody2D>().velocity);
			}
		}
	}

	void FixedUpdate() {
		if (!isPending)
			return;

		// 检测运动状态，当所有物体都停止运动时，切换回合
		GameObject[] actors = GameObject.FindGameObjectsWithTag ("player");
		int nPlayers = actors.Length;
		foreach (GameObject a in actors) {
			if (a.GetComponent<Rigidbody2D>().velocity != Vector2.zero) {
				isPending = true;
				return;
			}
		}
		
		actors = GameObject.FindGameObjectsWithTag ("enemy");
		int nEnemy = actors.Length;
		foreach (GameObject a in actors) {
			if (a.GetComponent<Rigidbody2D>().velocity != Vector2.zero) {
				isPending = true;
				return;
			}
		}

		if (nPlayers == 0) {
			Application.LoadLevel("lose");
			return;
		}

		if (nEnemy == 0) {
			Application.LoadLevel("win");
			return;
		}

		// TODO: 当前回合结束，发送READY给server
		isEnemyTurn = !isEnemyTurn;
		isPending = false;
		if (isEnemyTurn) {
			arrowEnemy.SetActive(true);
			arrowPlayer.SetActive(false);
		} else {
			arrowEnemy.SetActive(false);
			arrowPlayer.SetActive(true);
		}
		currentRound += 1;
		if (currentRound % 2 == 0)
			newRound ();
		if (isEnemyTurn)
			Debug.Log ("enemy turn");

		// 对手行动
		// 暂时用随机产生
		/*
		if (!isPending && isEnemyTurn) {

			int rand_x = Random.Range (0, 100);
			int rand_y = Random.Range (0, 100);
			int num = Random.Range (0, 3);
			GameObject enemyObj = GameObject.Find ("enemy" + num);
			Rigidbody2D rb = enemyObj.GetComponent<Rigidbody2D> ();
			rb.AddForce (new Vector2 (rand_x, rand_y));

			isPending = true;
		}
		*/
	}

	void OnPostRender() {
		if (!mouse_clicked || !playerReady) {
			return;
		}
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
		GL.Vertex(new Vector3(arrowEnd.x / Screen.width, arrowEnd.y / Screen.height, 0));
		GL.End();
		GL.PopMatrix();
	}

	void OnGUI()
	{
		if(Input.GetKeyDown(KeyCode.Escape)) {
			Application.LoadLevel("title");
		}

		GUIStyle style = new GUIStyle ();
		style.fontSize = 20;
		style.alignment = TextAnchor.LowerCenter;
		style.normal.textColor = Color.red;

		if (debugStr != null)
			GUI.Label(new Rect(100, 5, 100, 20), debugStr, style);

		if (!playerReady || !enemyReady) {

			if (!playerReady) {
				GUI.Label(new Rect(0, 40, Screen.width, 120), "xxx");
			} else {
				GUI.Label(new Rect(0, 40, Screen.width, 120), "等待对手开始");
			}
			if (GUI.Button (new Rect (Screen.width * 0.5f - 100, Screen.height * 0.5f - 150, 200, 30), "OK")) {
				// player is ready
				playerReady = true;
				// 等待对手就绪?

				// 暂时用随机生成的放置对手的小球
				int i;
				float rand_x, rand_y;
				for (i = 0; i < 3; i++) {
					// FIXME: 只能固定分辨率
					//rand_x = Random.Range (0, Screen.width);
					//rand_y = Random.Range (0, Screen.height);
					rand_x = (Random.Range (0, 480) - 240) / 100.0f;
					rand_y = Random.Range (0, 320) / 100.0f;

					//Vector2 rand_pos = Camera.main.ScreenToWorldPoint(new Vector2(rand_x, rand_y));
					Vector2 rand_pos = new Vector2(rand_x, rand_y);

					Transform new_ball = Instantiate(pieceTrans);
					new_ball.position = rand_pos;
					new_ball.name = "enemy" + i;
					HealthScript hs = new_ball.GetComponent<HealthScript>();
					hs.isEnemy = true;
					//SpriteRenderer render = new_ball.GetComponent<SpriteRenderer>();
					//render.sprite = ;
				}
				enemyReady = true;
			}
		
			if (GUI.Button (new Rect (Screen.width * 0.5f - 100, Screen.height * 0.5f - 100, 200, 30), "Cancel")) {
				// 返回标题画面
				Application.LoadLevel("title");
			}

		}
	}

	Vector2 randomPos()
	{
		int iCol, iRow;
		float iPosX, iPosY;

		while (true) {
			iCol = Random.Range (0, 9);
			iRow = Random.Range (0, 19);

			if (locationMap[iCol, iRow] == false)
				// if this position is not occupied
				break;
			locationMap[iCol, iRow] = true;
		}

		return grid2pos (iCol, iRow);
	}

	Vector2 grid2pos(int col, int row)
	{
		float x = ((col + 1.0f) * 36.0f - bgWidth / 2) / 100.0f;
		float y = ((row + 1.0f) * 32.0f - bgHeight / 2)/ 100.0f;

		return new Vector2 (x, y);
	}

	// 产生一个新的回合
	void newRound()
	{
		GameObject o;
		int i;

		// 检查道具效果是否到期，如果到期则取消
		for (i = 1; i <= 3; i++) {
		}

		// 清除当前场上剩余道具
		GameObject[] items = GameObject.FindGameObjectsWithTag ("items");
		foreach (GameObject item in items) {
			;
			Destroy (item);
		}

		// 随机生成新的道具
		for (i = 0; i < itemsPerRound; i ++) {
			int iType = Random.Range(0, itemPrefabs.Length);
			Vector2 pos = randomPos();
			//Vector2 pos = Camera.main.ScreenToWorldPoint(new Vector2(iPosX, iPosY));
			Transform iTransform = Instantiate(itemPrefabs[iType]);
			iTransform.position = pos;
		}
	}

	void pending()
	{
		isPending = true;
	}

	void turn()
	{
		isEnemyTurn = !isEnemyTurn;
	}
}