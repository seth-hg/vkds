using UnityEngine;
using System.Collections;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

/*
public static class preferences {
	// 可调整的参数
	public static float forceMax = 100.0f;		// 最大击球力度
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
*/

public class NetworkedScript : MonoBehaviour {
	
	public Material mat;
	public Color color = Color.red;
	
	// 随机生成player和enemy
	public Transform playerPrefab;
	public Transform enemyPrefab;

	// 用于每回合随机生成道具
	public Transform[] itemPrefabs;			// 动态道具，每回合按概率生成
	public Transform[] staticItemPrefabs;	// 静态道具，游戏开始时生成
	public ItemGenScript itemGenerator;
	private Vector2[] newItemPos;
	public bool[] createNewItem;

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
	private bool isSyncing = false;		// 等待服务器同步
	public int currentRound = 0;		// 当前回合数

	// drawing arrow & attack
	private bool mouse_clicked = false;
	private Vector2 arrowStart;
	private Vector2 arrowEnd;
	private Vector3 startVertex;

	//private bool playerReady = true, enemyReady = true;

	// 用于相机的缩放
	private Vector3 savedPos;
	private float cameraSize;
	
	private string debugStr = null;

	// 指示箭头
	private GameObject arrowPlayer, arrowEnemy;

	// 网络相关
	public string server_addr;
	public int server_port;
	private TcpClient client = null;
	private NetworkStream stream = null;
	private StreamReader sReader = null;
	private string roomID;
	private int playerID;
	
	string[] getResponse() {
		//byte[] buffer = new byte[128];
		//int ret = stream.Read(buffer, 0, 128);
		//string resp = Encoding.Default.GetString(buffer[0:ret]).TrimEnd("\n".ToCharArray());
		string resp = sReader.ReadLine ().TrimEnd('\n');
		Debug.Log ("resp: " + resp);
		return resp.Split(':');
	}

	bool sendJoin() {
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return false;
		}
		Debug.Log ("Sending JOIN to server");
		try {
			byte[] buffer = Encoding.Default.GetBytes("JOIN\n");
			stream.Write(buffer, 0, 5);
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
			return false;
		}
		
		try {
			string[] args = getResponse();
			//Debug.Log ("RET: " + args[0]);
			roomID = args[1];
			playerID = int.Parse(args[2]);
		} catch {
			Debug.Log ("failed recieving response");
		}
		
		return true;
	}
	
	int sendMove(int actor, int forceX, int forceY) {
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return -1;
		}
		Debug.Log ("Sending MOVE to server");
		try {
			string req = string.Format("MOVE:{0}:{1}:{2}:{3}:{4}\n", roomID, playerID, actor, forceX, forceY);
			Debug.Log (req);
			byte[] buffer = Encoding.Default.GetBytes(req);
			stream.Write(buffer, 0, req.Length);
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}
		
		try {
			string[] args = getResponse();
			//Debug.Log ("ok " + args[0]);
		} catch {
			Debug.Log ("failed recieving response");
		}
		return 0;
	}

	enum ret_t {
		ERR = -1,
		WAIT = 0,
		OK = 1,
		MOVE = 2,
		WAITP = 3,	// 等待另一玩家加入
		NEW = 4,	// 新回合
	}

	ret_t sendReady(out int actor, out int forceX, out int forceY) {
		actor = 0;
		forceX = 0;
		forceY = 0;
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return ret_t.ERR;
		}
		Debug.Log ("Sending READY to server");
		try {
			string req = string.Format("READY:{0}:{1}\n", roomID, playerID);
			Debug.Log (req);
			byte[] buffer = Encoding.Default.GetBytes(req);
			stream.Write(buffer, 0, req.Length);
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
			return ret_t.ERR;
		}
		
		try {
			string[] args = getResponse();
			//Debug.Log ("resp: " + args[0]);
			if (string.Compare(args[0], "OK") == 0) {
				// 本方执行操作
				isPending = false;
				isEnemyTurn = false;
				return ret_t.OK;
			} else if (string.Compare(args[0], "WAIT") == 0) {
				// 等待对手
				isPending = true;
				isEnemyTurn = true;
				isSyncing = true;
				return ret_t.WAIT;
			} else if (string.Compare(args[0], "MOVE") == 0) {
				// 对手执行了操作
				actor = int.Parse(args[1]);
				forceX = int.Parse(args[2]);
				forceY = int.Parse(args[3]);
				return ret_t.MOVE;
			} else if (string.Compare(args[0], "NEW") == 0) {
				//Debug.Log ("new items created");
				debugStr = "new items created";
				// 新回合开始，随机生成道具
				int gen, col, row;
				gen = int.Parse(args[1]);
				if (gen == 1) {
					col = int.Parse(args[2]);
					row = int.Parse(args[3]);
					if (playerID == 1) {
						col = 8 - col;
						row = 18 - row;
					}
					newItemPos[0] = itemGenerator.grid2pos(col, row);
					createNewItem[0] = true;
				}

				gen = int.Parse(args[4]);
				if (gen == 1) {
					col = int.Parse(args[5]);
					row = int.Parse(args[6]);
					if (playerID == 1) {
						col = 8 - col;
						row = 18 - row;
					}
					newItemPos[1] = itemGenerator.grid2pos(col, row);
					createNewItem[1] = true;
				}

				return ret_t.NEW;
			} else {
				return ret_t.ERR;
			}
		} catch {
			Debug.Log ("failed recieving response");
			return ret_t.ERR;
		}
	}

	void Start() {
		cameraSize = bgHeight / 200.0f;
		Camera.main.orthographicSize = cameraSize;

		forceFactor = preferences.forceMax / preferences.arrowMaxLen;

		itemGenerator = gameObject.GetComponent<ItemGenScript> ();

		// 生成静态道具
		for (int i = 0; i < staticItemPrefabs.Length; i++) {
			Transform iTransform = Instantiate(staticItemPrefabs[i]);
			if (playerID == 0)
				iTransform.position = itemGenerator.allocItem(false);
			else
				iTransform.position = itemGenerator.allocItem(true);
		}

		// 用于动态道具生成
		newItemPos = new Vector2[2];
		createNewItem = new bool[2];
		createNewItem [0] = false;
		createNewItem [1] = false;

		newRound ();

		//isEnemyTurn = Random.Range(0, 2) == 0;
		arrowPlayer = GameObject.Find ("arrowPlayer");
		arrowEnemy = GameObject.Find ("arrowEnemy");

		arrowPlayer.SetActive (false);
		arrowEnemy.SetActive (false);

		Debug.Log ("arrowMaxLen = " + preferences.arrowMaxLen);
		Debug.Log ("forceMax = " + preferences.forceMax);

		// 连接服务器
		client = new TcpClient ();
		Debug.Log ("connecting to server");
		try {
			client.Connect (server_addr, server_port);
			stream = client.GetStream();
			sReader = new StreamReader(stream);
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}

		debugStr = "joining a game ...";
		if (!sendJoin ()) {
			Debug.Log ("failed joining a game");
			// TODO: 处理错误
			return;
		}

		isSyncing = true;
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
		// 对手回合啥也不做
		if (isEnemyTurn || isPending)
			return;

		arrowEnd = Input.mousePosition;
		Vector2 dir = arrowStart - arrowEnd;
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

		/*
		// 初始布局，暂时没有使用
		if (mouse_clicked && !playerReady) {
			//Vector2 p = Input.mousePosition;
			Vector2 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			ball.transform.position = new Vector3(p.x, p.y, -5);
		}
		*/

		if (mouse_clicked && Input.GetMouseButtonUp (0)) {
			mouse_clicked = false;

			Camera.main.transform.position = savedPos;
			//Camera.main.orthographicSize = cameraSize;

			// 将用户操作发送到
			int actor = int.Parse(ball.transform.name.ToCharArray()[4].ToString());
			int forceX = (int)(dir.x * forceFactor);
			int forceY = (int)(dir.y * forceFactor);
			sendMove(actor, forceX, forceY);
			ball.GetComponent<HealthScript> ().Attack(new Vector2(forceX, forceY));
			isPending = true;
			//Debug.Log ("velocity: " + ball.GetComponent<Rigidbody2D>().velocity);
		}
	}

	void FixedUpdate() {
		if (isSyncing) {
			int actor, x, y;
			Debug.Log ("syncing ...");
			ret_t ret = sendReady(out actor, out x, out y);

			if (ret == ret_t.WAITP) {
				// TODO: 显示提示框
				debugStr = "waiting for player";
				isSyncing = true;
				return;
			}

			if (ret == ret_t.WAIT) {
				isSyncing = true;

				arrowEnemy.SetActive(true);
				arrowPlayer.SetActive(false);
				return;
			} else if (ret == ret_t.OK) {
				isEnemyTurn = false;
				isPending = false;
				isSyncing = false;
				arrowEnemy.SetActive(false);
				arrowPlayer.SetActive(true);
			} else if (ret == ret_t.MOVE) {
				// 对手执行了操作
				GameObject aObj = GameObject.Find("enemy" + actor);
				aObj.GetComponent<HealthScript> ().Attack(new Vector2(-x, -y));
				isPending = true;
				return;
			} else if (ret == ret_t.ERR) {
				// TODO: 处理错误
				return;
			}
		}

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

		currentRound += 1;
		if (currentRound % 2 == 0)
			newRound ();
		if (isEnemyTurn)
			Debug.Log ("enemy turn");

		isSyncing = true;

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
		if (!mouse_clicked) { // || !playerReady) {
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

		/* 出事布局界面，暂时未使用
		 */
		/*
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
		*/
	}

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

		// 清除当前场上剩余道具
		/*
		GameObject[] items = GameObject.FindGameObjectsWithTag ("items");
		foreach (GameObject item in items) {
			;
			Destroy (item);
		}
		*/

		// 随机生成新的道具
		for (i = 0; i < createNewItem.Length; i++) {
			//int p = Random.Range(0, 100);
			//if (p >= itemProb[i])
			if (!createNewItem[i])
				continue;
			//Vector2 pos = itemGenerator.allocItem();
			Vector2 pos = newItemPos[i];
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