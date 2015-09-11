using UnityEngine;
using System.Collections;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class NetworkedScript : MonoBehaviour {
	
	public Material mat;
	public Color color = Color.red;

	// 可调整的参数
	public float forceMax = 600.0f;      // 最大击球力度
	public float arrowMaxLen = 150.0f;   // 击球辅助线最大长度
	public float drag = 1.5f;

	// 随机生成player和enemy
	public Transform playerPrefab;
	public Transform enemyPrefab;

	// 用于每回合随机生成道具
	public Transform[] itemPrefabs;			// 动态道具，每回合按概率生成
	public int[] itemProbs;					// 动态道具出现的概率
	public int nStaticItems;				// 静态道具数目，放在数组最前面

	// 记录本方和对手actors
	private GameObject[] playerActors, enemyActors;

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
	private bool isWaiting = false;		// 等待对手消息
	private bool gameStarted = false;
	//public int currentRound = 0;		// 当前回合数

	// drawing arrow & attack
	private bool mouse_clicked = false;
	private Vector2 arrowStart;
	private Vector2 arrowEnd;
	private Vector3 startVertex;

	// 用于相机的缩放
	private Vector3 savedPos;
	private float cameraSize;
	
	private string debugStr = null;

	// 指示箭头
	private GameObject arrowPlayer, arrowEnemy;

	// 网络客户端
	public string serverAddr;
	public int serverPort;
	ClientScript client;

	private const int ST_WAIT 	= -1;	// 等待slave加入
	private const int ST_NEW 	= 0;	// 启动新回合
	private const int ST_MASTER = 1;	// master操作
	private const int ST_SLAVE 	= 2;	// slave操作

	private int gameStatus;		// 游戏当前的状态
	private bool isOperating;	// 是否用户操作
	private int fixedUpdateLoopCounter;	// 每10次FixedUpdate()调用拉取一次消息
	
	bool randomPos (float minDist, GameObject[] playerObjs, GameObject[] enemyObjs, GameObject[] itemObjs, out int x, out int y) {
		x = Random.Range(-15, 15);
		y = Random.Range(-30, 30);
		Vector2 pos = new Vector2(x / 10.0f, y / 10.0f);

		foreach (GameObject a in playerObjs) {
			if (a.activeInHierarchy && Vector2.Distance(pos, a.transform.position) < minDist)
				return false;
		}

		foreach (GameObject a in enemyObjs) {
			if (a.activeInHierarchy && Vector2.Distance(pos, a.transform.position) < minDist)
				return false;
		}

		foreach (GameObject a in itemObjs) {
			if (a.activeInHierarchy && Vector2.Distance(pos, a.transform.position) < minDist)
				return false;
		}
			
		return true;
	}

	void Start() {
		cameraSize = bgHeight / 200.0f;
		Camera.main.orthographicSize = cameraSize;

		forceFactor = forceMax / arrowMaxLen;

		// 连接服务器，加入游戏
		client = gameObject.GetComponent<ClientScript> ();
		if (client == null) {
			// TODO: 处理错误
		}
		client.Connect (serverAddr, serverPort);
		if (-1 == client.sendJoin ()) {
			// TODO: 处理发送JOIN错误
		}

		// Slave加入游戏后发送OK给master
		if (!client.isMaster ()) {
			Debug.Log ("this client is slave");
			client.sendOK();
		}
		//debugStr = "waiting for player ...";
		gameStatus = ST_WAIT;

		// 保存常用的GameObject
		// 保存actors
		playerActors = new GameObject[3];
		enemyActors = new GameObject[3];

		for (int i = 0; i < 3; i++) {
			GameObject obj;
			obj = GameObject.Find("ball" + (i+1));
			Debug.Assert(obj != null);
			playerActors[i] = obj;
			obj.SetActive(false);
			obj = GameObject.Find("enemy" + (i+1));
			Debug.Assert(obj != null);
			enemyActors[i] = obj;
			obj.SetActive(false);
		}

		// 指向箭头
		arrowPlayer = GameObject.Find ("arrowPlayer");
		arrowPlayer.SetActive (false);
		arrowEnemy = GameObject.Find ("arrowEnemy");
		arrowEnemy.SetActive (false);

		//isSyncing = true;
		isOperating = false;
		isWaiting = true;
		fixedUpdateLoopCounter = 0;

		return;
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

		// 等待对手消息中 ...
		if (!isOperating)
			return;

		arrowEnd = Input.mousePosition;
		Vector2 dir = arrowStart - arrowEnd;
		float len = Vector2.Distance(arrowStart, arrowEnd);

		if (len > arrowMaxLen) {
			dir.x = dir.x * arrowMaxLen / len;
			dir.y = dir.y * arrowMaxLen / len;
			arrowEnd = arrowStart + dir;
		}
		arrowEnd = arrowStart + dir;

		//if (!isPending && Input.GetMouseButtonDown (0)) {
		if (Input.GetMouseButtonDown (0)) {
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

		if (mouse_clicked && Input.GetMouseButtonUp (0)) {
			mouse_clicked = false;

			Camera.main.transform.position = savedPos;
			//Camera.main.orthographicSize = cameraSize;

			// 将用户操作发送到
			int actor = int.Parse(ball.transform.name.ToCharArray()[4].ToString());
			int forceX = (int)(dir.x * forceFactor);
			int forceY = (int)(dir.y * forceFactor);
			//sendMove(actor, forceX, forceY);
			ball.GetComponent<HealthScript> ().Attack(new Vector2(forceX, forceY));
			isPending = true;	// 等待actor运动结束

			// 发送操作给slave
			client.sendMove(actor, forceX, forceY);
			isWaiting = true;
			isOperating = false;
		}
	}

	void startGame() {
		GameObject.Find("DialogWaiting").GetComponent<WaitingScript>().hideDialog();

		foreach (GameObject a in playerActors)
			a.SetActive (true);
		foreach (GameObject a in enemyActors)
			a.SetActive (true);

		newRound(true);
		gameStarted = true;
	}

	void FixedUpdate() {
		debugStr = string.Format ("{0}:{1}:{2}", gameStatus.ToString (), isWaiting, isOperating);
		//Debug.Log ("FixedUpdate: " + isOperating + isWaiting);
		// 用户正在操作，跳过所有逻辑
		if (isOperating)
			return;

		if (gameStarted && isPending) {
			int nPlayers = 0, nEnemies = 0;
			foreach (GameObject a in playerActors) {
				if (a.activeInHierarchy == false)
					continue;
				nPlayers += 1;
				if (a.GetComponent<Rigidbody2D> ().velocity != Vector2.zero) {
					isPending = true;
					return;
				}
			}
			
			foreach (GameObject a in enemyActors) {
				if (a.activeInHierarchy == false)
					continue;
				nEnemies += 1;
				if (a.GetComponent<Rigidbody2D> ().velocity != Vector2.zero) {
					isPending = true;
					return;
				}
			}

			arrowPlayer.SetActive(false);
			arrowEnemy.SetActive(false);

			// 判断游戏结束
			if (nPlayers == 0) {
				client.sendClose();
				Application.LoadLevel ("lose");
			}
			
			if (nEnemies == 0) {
				client.sendClose();
				Application.LoadLevel ("win");
			}

			isPending = false;
			// 如果是当前客户端为master，并且slave操作刚刚结束，则开始一个新回合
			if (client.isMaster() && gameStatus == ST_NEW) {
				newRound(false);
				isWaiting = true;
				isOperating = false;
				return;
			}
		}

		//fixedUpdateLoopCounter += 1;
		if (isWaiting) {
			if (++fixedUpdateLoopCounter % 30 != 0) {
				return;
			}
			fixedUpdateLoopCounter = 0;
			//Debug.Log ("pulling msg ... ");
			string[] resp;
			// 从服务器拉取消息
			if (!client.pullMsg(out resp)) {
				// 无消息，返回继续等待
				return;
			}

			if (resp[0] == "OK") {
				Debug.Assert(client.isMaster());
				//gameStatus += 1;	// master进入下一阶段
				Debug.Log ("slave confirmed, move onto next stage");
				gameStatus = (gameStatus + 1) % 3;
				// master发布下一阶段指令
				switch (gameStatus) {
				case ST_NEW:
					// slave加入，master开始游戏第一回合
					// 隐藏等待窗口
					startGame();
					isWaiting = true;
					isOperating = false;
					break;
				case ST_MASTER:
					// master操作
					isOperating = true;
					isWaiting = false;
					arrowPlayer.SetActive(true);
					break;
				case ST_SLAVE:
					// 轮到slave操作，发送YOURS，等待响应
					client.sendYours();
					isWaiting = true;
					arrowEnemy.SetActive(true);
					break;
				default:
					// 无效状态
					Debug.Log ("shouldn't be here");
					Debug.Assert(false);
					break;
				}
				return;
			} else if (resp[0] == "NEW") {
				Debug.Assert(!client.isMaster());
				// 收到master命令，开始新回合，创建动态道具
				if (gameStarted == false) {
					// 首回合
					startGame();
				}
				int len = resp.Length;
				for (int i = 3; i < len; i += 3) {
					int itemID = int.Parse(resp[i]);
					// master和slave屏幕方向相反，因此坐标取反
					float posX = int.Parse(resp[i+1]) / -10.0f;
					float posY = int.Parse(resp[i+2]) / -10.0f;

					Transform iTrans = Instantiate(itemPrefabs[itemID]);
					iTrans.position = new Vector2(posX, posY);
				}
				// 发送OK响应master
				client.sendOK();
			} else if (resp[0] == "MOVE") {
				// 解析对手操作
				int actorID, forceX, forceY;

				try {
					actorID = int.Parse(resp[3]) - 1;
					forceX = - int.Parse(resp[4]);
					forceY = - int.Parse(resp[5]);

					GameObject actor = enemyActors[actorID];
					actor.GetComponent<HealthScript>().Attack(new Vector2(forceX, forceY));
					//actor.GetComponent<Rigidbody2D>().AddForce();
					isOperating = false;
					isWaiting = false;
					isPending = true;
					// slave收到master的操作后响应OK
					if (!client.isMaster())
						client.sendOK();
				} catch {
					// 消息解析错误
					Debug.Log ("invalid msg ...");
					Debug.Assert(false);
				}

				// 老鼠运动刚刚开始，不需要后面的检查过程
				if (client.isMaster()) {
					// 如果本机是master，则对手刚刚执行完操作，
					// 等待所有actor停止运动然后开始新回合
					gameStatus = ST_NEW;
				} else {
					// 如果本机是slave，则等待master的下一步指令
					isWaiting = true;
				}
				return;
			} else if (resp[0] == "YOURS") {
				Debug.Assert (!client.isMaster());
				isOperating = true;
				isPending = false;
				isWaiting = false;
				return;
			} else if (resp[0] == "CLOSED") {
				// 对方退出或掉线，游戏结束，自动获胜
				isWaiting = false;
				Application.LoadLevel("win");
			} else {
				// 无效消息，尝试重新获取，其实并没有什么卵用，因为服务器和对手都不会重发
				Debug.Log ("invalid msg: " + resp[0]);
				isWaiting = true;
			}
		}

		// slave等待master发布下一阶段指令
		if (!client.isMaster ()) {
			isWaiting = true;
		} else {

		}

		// 老鼠运动都结束，游戏进入下一阶段
		//gameStatus = (gameStatus + 1) % 3;

		return;
	}

	void OnPostRender() {
		if (!mouse_clicked) {
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
	}

	// 产生一个新的回合
	void newRound(bool withStatic)
	{
		// 检查道具效果是否到期，如果到期则取消
		//GameObject[] playerActors = GameObject.FindGameObjectsWithTag("player");
		foreach (GameObject a in playerActors)
		{
			// 跳过inactive（已经死掉）的对象
			if (a.activeInHierarchy == false)
				continue;
			HealthScript hs = a.GetComponent<HealthScript>();
			if (hs.attackEnhanceTurnLeft > 0) {
				hs.attackEnhanceTurnLeft -= 1;
				if (hs.attackEnhanceTurnLeft == 0) {
					hs.attack = 0;
					hs.animator.SetBool("isFire", false);
				}
			}
		}
		
		//GameObject[] enemyActors = GameObject.FindGameObjectsWithTag("enemy");
		foreach (GameObject a in enemyActors)
		{
			// 跳过inactive（已经死掉）的对象
			if (a.activeInHierarchy == false)
				continue;
			HealthScript hs = a.GetComponent<HealthScript>();
			if (hs.attackEnhanceTurnLeft > 0) {
				hs.attackEnhanceTurnLeft -= 1;
				if (hs.attackEnhanceTurnLeft == 0) {
					hs.attack = 0;
					hs.animator.SetBool("isFire", false);
				}
			}
		}
		
		GameObject[] itemObjects = GameObject.FindGameObjectsWithTag("items");
		foreach (GameObject a in itemObjects) {
			ItemScript iScript = a.GetComponent<ItemScript> ();
			if (iScript == null) {
				Debug.Log ("not an item: " + a);
				continue;
			}
			if (iScript.isStatic)
				continue;
			iScript.availableTurnsLeft -= 1;
			if (iScript.availableTurnsLeft == 0)
				Destroy(a);
		}

		// master负责生成道具，slave跳过次步骤
		if (client.isMaster ()) {
			// 重新获取一次items
			itemObjects = GameObject.FindGameObjectsWithTag ("items");

			// 随机生成新的道具
			int[] outX, outY;
			bool[] outFlags;
			int nItems = 0;
			outFlags = new bool[itemProbs.Length];
			outX = new int[itemProbs.Length];
			outY = new int[itemProbs.Length];

			// 道具0和1是静态道具，动态道具从2开始
			int i = withStatic ? 0 : nStaticItems;
			for (; i < itemProbs.Length; i++) {
				outFlags [i] = false;
				int p = Random.Range (0, 100);
				if (p >= itemProbs [i])
					continue;

				outFlags [i] = true;
				while (!randomPos(1.2f, playerActors, enemyActors, itemObjects, out outX[i], out outY[i]))
					;
				Vector2 pos = new Vector2 (outX [i] / 10.0f, outY [i] / 10.0f);
				Transform iTransform = Instantiate (itemPrefabs [i]);
				iTransform.position = pos;
				nItems += 1;
			}
			// 发送道具类型和位置给slave
			client.sendNew (nItems, outFlags, outX, outY);
		}
	}
}
