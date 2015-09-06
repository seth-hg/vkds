using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Text;

public class NetworkDemo : MonoBehaviour {

	private TcpClient client = null;
	private NetworkStream stream = null;
	private string roomID;
	private int playerID;

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
			byte[] buffer = new byte[128];
			int ret = stream.Read(buffer, 0, 128);
			string resp = Encoding.Default.GetString(buffer);
			string[] args = resp.Split(':');
			Debug.Log ("RET: " + resp);
			roomID = args[1];
			playerID = int.Parse(args[2]);
		} catch {
			Debug.Log ("failed recieving response");
		}

		return true;
	}

	int sendMove(int forceX, int forceY) {
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return -1;
		}
		Debug.Log ("Sending MOVE to server");
		try {
			string req = string.Format("MOVE:{0}:{1}:{2}:{3}\n", roomID, playerID, forceX, forceY);
			Debug.Log (req);
			byte[] buffer = Encoding.Default.GetBytes(req);
			stream.Write(buffer, 0, 5);
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}
		
		try {
			byte[] buffer = new byte[128];
			int ret = stream.Read(buffer, 0, 128);
			string[] resp = Encoding.Default.GetString(buffer).Split(':');
			Debug.Log ("ok " + resp[0]);
		} catch {
			Debug.Log ("failed recieving response");
		}
		return 0;
	}

	int sendReady() {
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return -1;
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
		}
		
		try {
			byte[] buffer = new byte[128];
			int ret = stream.Read(buffer, 0, 128);
			//string[] resp = Encoding.Default.GetString(buffer).Split(':');
			Debug.Log ("ok " + Encoding.Default.GetString(buffer));
		} catch {
			Debug.Log ("failed recieving response");
		}

		return 0;
	}

	void Start ()
	{
		client = new TcpClient ();
	}

	public void onClickConnect() {
		Debug.Log ("connecting to server");
		try {
			client.Connect ("127.0.0.1", 8888);
			stream = client.GetStream();
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}
	}

	public void onClickJoin () {
		sendJoin ();
	}

	public void onClickDo () {
		sendMove (10, 10);
	}

	public void onClickReady () {
		sendReady ();
	}
}
