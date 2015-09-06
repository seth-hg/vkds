using UnityEngine;
using System.Collections;

public static class preferences {
	// 可调整的参数
	public static float forceMax = 200.0f;		// 最大击球力度
	public static float arrowMaxLen = 200.0f;	// 击球辅助线最大长度
	public static float drag = 2.0f;

	// 各项参数的可调整范围
	public static float forceMax_Max = 500.0f;
	public static float forceMax_Min = 50.0f;
	public static float arrowMaxLen_Max = 500.0f;
	public static float arrowMaxLen_Min = 100.0f;
	public static float drag_Max = 10.0f;
	public static float drag_Min = 1.0f;
}

public class itemAllocator {
	private bool[,] locationMap; 
	private int Cols, Rows;
	private int bgWidth, bgHeight;			// 背景尺寸
	private int gridWidth, gridHeight;		// 网格尺寸
	
	public void initMap(int nCols, int nRows, int width, int height) {
		int col, row;
		locationMap = new bool[nCols, nRows];
		for (col = 0; col < nCols; col ++)
			for (row = 0; row < nRows; row ++)
				locationMap [col, row] = false;
		Cols = nCols;
		Rows = nRows;
		bgWidth = width;
		bgHeight = height;
		
		// 初始化道具网格
		gridWidth = bgWidth / 10;
		gridHeight = bgHeight / 20;
	}

	public Vector2 allocItem () {
		int iCol, iRow;
		
		while (true) {
			iCol = Random.Range (0, Cols);
			iRow = Random.Range (0, Rows);
			
			if (available(iCol, iRow)) {
				// if this position is not occupied
				break;
			}
		}
		markLocation (iCol, iRow);

		return grid2pos(iCol, iRow);
	}

	bool available(int col, int row) {
		return locationMap [col, row] == false;
	}

	void markLocation (int col, int row) {
		locationMap [col, row] = true;
	}

	void clearLocation (int col, int row) {
		locationMap [col, row] = false;
	}

	Vector2 grid2pos(int col, int row) {
		float x = ((col + 1.0f) * gridWidth - bgWidth / 2) / 100.0f;
		float y = ((row + 1.0f) * gridHeight - bgHeight / 2)/ 100.0f;
		
		return new Vector2 (x, y);
	}
	
	Vector2 pos2grid(float x, float y) {
		float col, row;
		
		x = x - gridWidth / 2;
		y = y - gridHeight / 2;
		if (x < 0)
			col = 0;
		else
			col = x / gridWidth;
		
		if (y < 0)
			row = 0;
		else
			row = y / gridHeight;
		
		return new Vector2 (col, row);
	}

	public void markPosition (Vector2 pos) {
		Vector2 loc = pos2grid (pos.x, pos.y);
		markLocation ((int)loc.x, (int)loc.y);
	}

	public void clearPosition (Vector2 pos) {
		Vector2 loc = pos2grid (pos.x, pos.y);
		clearLocation ((int)loc.x, (int)loc.y);
	}
}

public class CameraScript : MonoBehaviour {
	
	public Material mat;
	public Color color = Color.red;

	// 随机生成player和enemy
	public Transform playerPrefab;
	public Transform enemyPrefab;
	// 用于每回合随机生成道具
	public Transform[] itemPrefabs;		// 道具perfabs
	public int[] itemProb;				// 各种道具的出现概率

	// 背景图片的尺寸
	public int bgWidth;
	public int bgHeight;
	
	private float forceFactor;			// 击球力量系数

	// selecting ball
	private RaycastHit2D hit;
	private GameObject ball;			// 当前被选中的小球

	//
	private bool isEnemyTurn = false;	// 对手回合
	private bool isPending = false;		// 等待回合结束(所有物体停止运动)
	public int currentRound = 0;		// 当前回合数

	// 用于绘制辅助线
	private bool mouse_clicked = false;
	private Vector2 arrowStart;
	private Vector2 arrowEnd;
	private Vector3 startVertex;

	private bool playerReady = true, enemyReady = true;

	// 用于相机的缩放
	private Vector3 savedPos;
	private float cameraSize;
	
	private string debugStr = null;

	// 指示箭头
	private GameObject arrowPlayer, arrowEnemy;

	//private bool[,] locationMap; 
	//public itemAllocator iAllocator;
	public ItemGenScript itemGenerator;

	void Start() {
		cameraSize = bgHeight / 200.0f;
		//cameraSize = 3.2f;
		Camera.main.orthographicSize = cameraSize;
		debugStr = cameraSize.ToString ();

		//preferences.arrowMaxLen = Screen.height / 5.0f;

		forceFactor = preferences.forceMax / preferences.arrowMaxLen;

		/*
		iAllocator = new itemAllocator ();
		iAllocator.initMap (9, 19, bgWidth, bgHeight);

		// 标记Actor所在的位置
		GameObject[] actors = GameObject.FindGameObjectsWithTag ("player");
		foreach (GameObject a in actors) {
			iAllocator.markPosition(a.transform.position);
		}
		
		actors = GameObject.FindGameObjectsWithTag ("enemy");
		foreach (GameObject a in actors) {
			iAllocator.markPosition(a.transform.position);
		}
		*/
		itemGenerator = gameObject.GetComponent<ItemGenScript> ();

		newRound ();

		isEnemyTurn = Random.Range(0, 2) == 0;
		arrowPlayer = GameObject.Find ("arrowPlayer");
		arrowEnemy = GameObject.Find ("arrowEnemy");
		if (isEnemyTurn) {
			arrowPlayer.SetActive (false);
		} else {
			arrowEnemy.SetActive (false);
		}

		Debug.Log ("arrowMaxLen = " + preferences.arrowMaxLen);
		Debug.Log ("forceMax = " + preferences.forceMax);
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
		//float angle = Vector2.Angle(dir, new Vector2(1, 0));
		float len = Vector2.Distance(arrowStart, arrowEnd);

		if (len > preferences.arrowMaxLen) {
			dir.x = dir.x * preferences.arrowMaxLen / len;
			dir.y = dir.y * preferences.arrowMaxLen / len;
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
				// 只在当前小球靠近屏幕边缘的时候调整camera
				savedPos = Camera.main.transform.position;
				Vector2 ballPos = ball.transform.position;
				//float newX, newY;
				if (ballPos.x < bgWidth * (-0.6f) / 200.0f || ballPos.x > bgWidth * 0.6f / 200.0f ||
				    ballPos.y < bgHeight * (-0.6f) / 200.0f || ballPos.y > bgWidth * 0.6f / 200.0f) {
					//Camera.main.orthographicSize = cameraSize / 1.5f;
					Camera.main.transform.position = new Vector3(ball.transform.position.x, ball.transform.position.y, -10);
				}

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

				Camera.main.transform.position = savedPos;
				//Camera.main.orthographicSize = cameraSize;
				itemGenerator.clearPosition (ball.transform.position);
				ball.GetComponent<HealthScript> ().Attack(dir);
				isPending = true;
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

					Transform new_ball = Instantiate(enemyPrefab);
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

	/*
	Vector2 randomPos()
	{
		int iCol, iRow;

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
	*/
	
	// 产生一个新的回合
	void newRound()
	{
		int i;

		// 检查道具效果是否到期，如果到期则取消
		GameObject[] actors = GameObject.FindGameObjectsWithTag ("player");
		foreach (GameObject a in actors) {
			HealthScript hs = a.GetComponent<HealthScript>();
			if (hs.attackEnhanceTurnLeft > 0)
				hs.attackEnhanceTurnLeft -= 1;
			if (hs.attackEnhanceTurnLeft == 0)
				hs.attack = 0;
		}
		
		actors = GameObject.FindGameObjectsWithTag ("enemy");
		foreach (GameObject a in actors) {
			HealthScript hs = a.GetComponent<HealthScript>();
			if (hs.attackEnhanceTurnLeft > 0)
				hs.attackEnhanceTurnLeft -= 1;
			if (hs.attackEnhanceTurnLeft == 0)
				hs.attack = 0;
		}

		// 随机生成新的道具
		for (i = 0; i < itemProb.Length; i++) {
			int p = Random.Range(0, 100);
			if (p >= itemProb[i])
				continue;
			Vector2 pos = itemGenerator.allocItem(false);
			//Vector2 pos = Camera.main.ScreenToWorldPoint(new Vector2(iPosX, iPosY));
			Transform iTransform = Instantiate(itemPrefabs[i]);
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