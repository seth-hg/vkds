using UnityEngine;
using System.Collections;

public class CameraScript : MonoBehaviour
{

    public Material mat;
    public Color color = Color.red;

	// 可调整的参数
	public float forceMax = 600.0f;      // 最大击球力度
	public float arrowMaxLen = 150.0f;   // 击球辅助线最大长度
	public float drag = 1.5f;

	// 随机生成player和enemy
    public Transform playerPrefab;
    public Transform enemyPrefab;

    // 用于合随机生成道具
    public Transform[] itemPrefabs;         // 动态道具，每回合按概率生成
    public int[] itemProb;              	// 道具的出现概率
    public int nStaticItems;				// 静态道具数目

    // 背景图片的尺寸
    public int bgWidth;
    public int bgHeight;

    private float forceFactor;          // 击球力量系数

    // selecting ball
    private RaycastHit2D hit;
    private GameObject ball;            // 当前被选中的小球

    //
    private bool isEnemyTurn = false;   // 对手回合
    private bool isPending = false;     // 等待回合结束(所有物体停止运动)
    private int currentRound = 0;        // 当前回合数

    // 用于绘制辅助线
    private bool mouse_clicked = false;
    private Vector2 arrowStart;
    private Vector2 arrowEnd;
    private Vector3 startVertex;

    //private bool playerReady = true, enemyReady = true;

    // 用于相机的移动和缩放
    private Vector3 savedPos;
    private float cameraSize;

    private string debugStr = null;

    // 指示箭头
    private GameObject arrowPlayer, arrowEnemy;

    void Start()
    {
        cameraSize = bgHeight / 200.0f;
        Camera.main.orthographicSize = cameraSize;
        //debugStr = cameraSize.ToString ();

        //preferences.arrowMaxLen = Screen.height / 5.0f;

        forceFactor = forceMax / arrowMaxLen;

		/*
		// 生成静态道具（prick, trap）
		foreach (Transform t in staticItemPrefabs) {
			Transform iTrans = Instantiate(t);
			iTrans.position = randomPos(5.0f, GameObject.FindGameObjectsWithTag("player"), GameObject.FindGameObjectsWithTag("enemy"), GameObject.FindGameObjectsWithTag("items"));
		}
		*/

        newRound(true);

        isEnemyTurn = Random.Range(0, 2) == 0;
        arrowPlayer = GameObject.Find("arrowPlayer");
        arrowEnemy = GameObject.Find("arrowEnemy");

        if (isEnemyTurn) {
            arrowPlayer.SetActive(false);
        } else {
            arrowEnemy.SetActive(false);
        }

        //Debug.Log("arrowMaxLen = " + arrowMaxLen);
        //Debug.Log("forceMax = " + forceMax);
    }

    // Update is called once per frame
    void Update()
    {
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

        if (len > arrowMaxLen)
        {
            dir.x = dir.x * arrowMaxLen / len;
            dir.y = dir.y * arrowMaxLen / len;
            arrowEnd = arrowStart + dir;
        }
        arrowEnd = arrowStart + dir;

        //if (!isPending && !isEnemyTurn && Input.GetMouseButtonDown (0)) {
        if (!isPending && Input.GetMouseButtonDown(0))
        {
            hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {
                //ts_mouse_start = System.DateTime.Now;
                ball = GameObject.Find(hit.transform.name);
                HealthScript hs = ball.GetComponent<HealthScript>();
                if (!hs || hs.isEnemy != isEnemyTurn)
                {
                    return;
                }

                hs.mouseAudio.PlayOneShot(hs.mouseSelect, 1f);
                mouse_clicked = true;
                // 只在当前小球靠近屏幕边缘的时候调整camera
                savedPos = Camera.main.transform.position;
                Vector2 ballPos = ball.transform.position;
                //float newX, newY;
                if (ballPos.x < bgWidth * (-0.6f) / 200.0f || ballPos.x > bgWidth * 0.6f / 200.0f ||
                    ballPos.y < bgHeight * (-0.6f) / 200.0f || ballPos.y > bgWidth * 0.6f / 200.0f)
                {
                    //Camera.main.orthographicSize = cameraSize / 1.5f;
                    Camera.main.transform.position = new Vector3(ball.transform.position.x, ball.transform.position.y, -10);
                }

                arrowStart = Camera.main.WorldToScreenPoint(ball.transform.position);
                startVertex = new Vector3(arrowStart.x / Screen.width, arrowStart.y / Screen.height, 0);
                arrowEnd = arrowStart;
            }
        }

        // 暂时并未使用
        /*
		if (mouse_clicked && !playerReady) {
			//Vector2 p = Input.mousePosition;
			Vector2 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
			ball.transform.position = new Vector3(p.x, p.y, -5);
		}
		*/

        if (mouse_clicked && Input.GetMouseButtonUp(0))
        {
            mouse_clicked = false;
            //if (playerReady && enemyReady) { // if battle started

            dir.x = dir.x * forceFactor;
            dir.y = dir.y * forceFactor;

            Camera.main.transform.position = savedPos;
            //Camera.main.orthographicSize = cameraSize;
            //itemGenerator.clearPosition(ball.transform.position);
            ball.GetComponent<HealthScript>().Attack(dir);
            isPending = true;
            //}
        }
    }

    void FixedUpdate()
    {
        if (!isPending)
            return;

        // 检测运动状态，当所有物体都停止运动时，切换回合
        GameObject[] actors = GameObject.FindGameObjectsWithTag("player");
        int nPlayers = actors.Length;
        foreach (GameObject a in actors)
        {
            if (a.GetComponent<Rigidbody2D>().velocity != Vector2.zero)
            {
                isPending = true;
                return;
            }
        }

        actors = GameObject.FindGameObjectsWithTag("enemy");
        int nEnemy = actors.Length;
        foreach (GameObject a in actors)
        {
            if (a.GetComponent<Rigidbody2D>().velocity != Vector2.zero)
            {
                isPending = true;
                return;
            }
        }

        if (nPlayers == 0)
        {
            Application.LoadLevel("bluewin");
            return;
        }

        if (nEnemy == 0)
        {
            Application.LoadLevel("redwin");
            return;
        }

        // TODO: 当前回合结束，发送READY给server
        isEnemyTurn = !isEnemyTurn;
        isPending = false;
        if (isEnemyTurn)
        {
            arrowEnemy.SetActive(true);
            arrowPlayer.SetActive(false);
        }
        else
        {
            arrowEnemy.SetActive(false);
            arrowPlayer.SetActive(true);
        }
        currentRound += 1;
        if (currentRound % 2 == 0)
            newRound(false);
        if (isEnemyTurn)
            Debug.Log("enemy turn");
    }

    void OnPostRender()
    {
        if (!mouse_clicked)
        { // || !playerReady) {
            return;
        }
        if (!mat)
        {
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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.LoadLevel("title");
        }

        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.alignment = TextAnchor.LowerCenter;
        style.normal.textColor = Color.red;

        if (debugStr != null)
            GUI.Label(new Rect(100, 5, 100, 20), debugStr, style);
    }

	/*
	Vector2 randomPos (float minDist, GameObject[] playerObjs, GameObject[] enemyObjs, GameObject[] itemObjs) {
		while (true) {
			int x, y;
			//x = Random.Range(Screen.width / 5, Screen.width * 4 / 5);
			//y = Random.Range(Screen.height / 5, Screen.height * 4 / 5);
			x = Random.Range(-15, 15);
			y = Random.Range(-30, 30);
			Vector2 pos = new Vector2(x / 10.0f, y / 10.0f);
			//Vector2 pos = Camera.main.ScreenToWorldPoint(new Vector2(x, y));

			foreach (GameObject a in playerObjs) {
				if (Vector2.Distance(pos, a.transform.position) < minDist)
					continue;
			}
			
			foreach (GameObject a in enemyObjs) {
				if (Vector2.Distance(pos, a.transform.position) < minDist)
					continue;
			}
			
			foreach (GameObject a in itemObjs) {
				if (Vector2.Distance(pos, a.transform.position) < minDist)
					continue;
			}
			
			return pos;
		}
	}
	*/

	bool randomPos (float minDist, GameObject[] playerObjs, GameObject[] enemyObjs, GameObject[] itemObjs, out int x, out int y) {
		x = Random.Range(-15, 15);
		y = Random.Range(-30, 30);
		Vector2 pos = new Vector2(x / 10.0f, y / 10.0f);
		
		foreach (GameObject a in playerObjs) {
			if (a != null && Vector2.Distance(pos, a.transform.position) < minDist)
				return false;
		}
		
		foreach (GameObject a in enemyObjs) {
			if (a != null && Vector2.Distance(pos, a.transform.position) < minDist)
				return false;
		}
		
		foreach (GameObject a in itemObjs) {
			if (a != null && Vector2.Distance(pos, a.transform.position) < minDist)
				return false;
		}
		
		return true;
	}

    void newRound(bool withStatic)
    {
        // 检查道具效果是否到期，如果到期则取消
        GameObject[] playerActors = GameObject.FindGameObjectsWithTag("player");
        foreach (GameObject a in playerActors)
        {
            HealthScript hs = a.GetComponent<HealthScript>();
            if (hs.attackEnhanceTurnLeft > 0) {
                hs.attackEnhanceTurnLeft -= 1;
            	if (hs.attackEnhanceTurnLeft == 0) {
                	hs.attack = 0;
					hs.animator.SetBool("isFire", false);
				}
			}
        }

        GameObject[] enemyActors = GameObject.FindGameObjectsWithTag("enemy");
        foreach (GameObject a in enemyActors)
        {
            HealthScript hs = a.GetComponent<HealthScript>();
            if (hs.attackEnhanceTurnLeft > 0) {
                hs.attackEnhanceTurnLeft -= 1;
            	if (hs.attackEnhanceTurnLeft == 0) {
                	hs.attack = 0;
					hs.animator.SetBool("isFire", false);
				}
			}
        }

		// 检查道具是否过期
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

        // 随机生成新的道具
		int i = withStatic ? 0 : nStaticItems;
        for (; i < itemProb.Length; i++)
        {
            int p = Random.Range(0, 100);
            if (p >= itemProb[i])
                continue;

			itemObjects = GameObject.FindGameObjectsWithTag("items");

			int outX, outY;
			while (!randomPos(1.2f, playerActors, enemyActors, itemObjects, out outX, out outY));
			Vector2 pos = new Vector2(outX / 10.0f, outY / 10.0f);
			Transform iTransform = Instantiate(itemPrefabs[i]);
			iTransform.position = pos;
        }
    }
}