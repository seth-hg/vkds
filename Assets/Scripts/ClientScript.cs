using UnityEngine;
using System.Collections;

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;

public class ClientScript : MonoBehaviour {

	private string roomID;
	private int playerID;

	private bool gameCreated;
	private bool gameJoined;

	private TcpClient client = null;
	private NetworkStream stream = null;
	private StreamReader sReader = null;

	public bool isMaster() {
		return playerID == 0;
	}

	// 连接服务器
	public int Connect(string serverAddr, int serverPort) {

		client = new TcpClient ();
		//Debug.Log ("connecting to server");
		try {
			client.Connect (serverAddr, serverPort);
			stream = client.GetStream();
			sReader = new StreamReader(stream);
			//Debug.Log ("ok");
		} catch {
			Debug.Log ("failed connecting server");
			return -1;
		}

		return 0;
	}

	string getResponse() {
		string resp;
		try {
			resp = sReader.ReadLine ().TrimEnd('\n');
		} catch {
			Debug.Log ("failed receiving response");
			return "";
		}

		Debug.Log ("recv: " + resp);
		return resp;
	}

	int sendMsg(string req) {
		Debug.Log ("send: " + req);
		try {
			byte[] buffer = Encoding.Default.GetBytes(req);
			stream.Write(buffer, 0, req.Length);
			//Debug.Log ("ok");
		} catch {
			Debug.Log ("failed sending msg: " + req);
			return -1;
		}

		return 0;
	}

	// 发送一个请求并返回响应
	string sendrecv(string req) {
		// TODO: 处理错误
		sendMsg (req);

		return getResponse ();
	}

	public int sendJoin() {
		string resp = sendrecv ("JOIN\n");
		string[] args = resp.Split (':');

		if (args[0] != "JOIN") {
			Debug.Log ("failed joining game");
			return -1;
		}

		try {
			roomID = args [1];
			playerID = int.Parse (args[2]);
		} catch {
			Debug.Log ("invalid response for JOIN");
			return -1;
		}

		return -1;
	}

	// master only. 发送随机道具的位置
	public int sendNew(int n, bool[] flags, int[] x, int[] y) {

		string req = string.Format("NEW:{0}:{1}", roomID, playerID);
		string[] args = new string[n * 3];

		// 三者必须相等
		Debug.Assert (flags.Length == x.Length);
		Debug.Assert (x.Length == y.Length);

		int j = 0;
		for (int i = 0; i < flags.Length; i++) {
			if (flags[i] == false)
				continue;
			args[j * 3 + 0] = i.ToString();
			args[j * 3 + 1] = x[i].ToString();
			args[j * 3 + 2] = y[i].ToString();
			j += 1;
		}

		if (j > 0) {
			req = req + ":" + string.Join (":", args) + "\n";
		}

		string[] resp = sendrecv (req).Split (':');

		if (resp[0] != "WAIT") {
			Debug.Log ("failed receiving response from server");
			return -1;
		}

		return 0;
	}

	private string sendrecvMsgNoArgs (string cmd) {
		string req = string.Format ("{0}:{1}:{2}\n", cmd, roomID, playerID);
		return sendrecv (req);
	}

	// 从服务器拉去对手消息
	public bool pullMsg(out string[] resp) {
		resp = sendrecvMsgNoArgs ("READY").Split(':');

		if (resp [0] == "WAIT") {
			// 没有消息，继续等待
			return false;
		}

		return true;
	}

	/*
	public int pullSlaveMsg(out string[] resp) {
		Debug.Assert (playerID == 0);

		resp = sendrecvMsgNoArgs ("READY").Split(':');

		// handle response
		if (resp [0] == "WAIT") {
			// 没有slave消息，继续等待
		} else if (resp [0] == "OK") {
			// slave确认消息，进入下一步
		} else if (resp [0] == "MOVE") {
			// slave执行一次操作
		} else if (resp [0] == "CLOSED") {
			// slave退出，游戏关闭
		} else {
			// 无效消息，等待重试
		}

		return -1;
	}

	public int pullMasterMsg(out string[] resp) {
		Debug.Assert (playerID == 1);

		resp = sendrecvMsgNoArgs ("READY").Split(':');

		if (resp [0] == "WAIT") {
			// 没有master消息，继续等待
			return RET_T.WAIT;
		} else if (resp [0] == "NEW") {
			// 开始新回合，重新生成动态道具
			return RET_T.NEW;
		} else if (resp [0] == "MOVE") {
			// master执行了操作
		} else if (resp [0] == "YOURS") {
			// 轮到slave操作
		} else if (resp [0] == "CLOSED") {
			// master退出，关闭游戏
		} else {
			// 无效消息，等待重试
		}

		return -1;
	}
	*/

	// 发送玩家操作
	public int sendMove(int actorID, int forceX, int forceY) {
		string req;

		try {
			req = string.Format ("MOVE:{0}:{1}:{2}:{3}:{4}\n", roomID, playerID, actorID, forceX, forceY);
		} catch {
			Debug.Log ("sending move failed");
			return -1;
		}

		string[] resp = sendrecv (req).Split(':');

		if (resp[0] != "WAIT") {
			Debug.Log ("failed receiving response from server");
			return -1;
		}

		return 0;
	}

	// master only. 发送YOURS命令，通知slave进行操作
	public int sendYours() {
		Debug.Assert (playerID == 0);

		string[] resp = sendrecvMsgNoArgs ("YOURS").Split(':');

		if (resp[0] != "WAIT") {
			Debug.Log ("failed receiving response from server");
			return -1;
		}

		return 0;
	}

	// slave only. 发送OK，响应master消息
	public int sendOK() {
		Debug.Assert (playerID == 1);

		string[] resp = sendrecvMsgNoArgs ("OK").Split(':');

		if (resp[0] != "WAIT") {
			Debug.Log ("failed receiving response from server");
			return -1;
		}

		return 0;
	}

	// 发送close关闭游戏
	public int sendClose() {

		sendMsg (string.Format ("CLOSE:{0}:{1}\n", roomID, playerID));
		
		return 0;
	}

	// 发送close关闭游戏
	public int sendCancel() {
		
		sendMsg (string.Format ("CANCEL:{0}:{1}\n", roomID, playerID));
		
		return 0;
	}
}
