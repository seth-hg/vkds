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
	//private byte[] buffer = new byte[1024];

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
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return;
		}
		Debug.Log ("Sending JOIN to server");
		try {
			byte[] buffer = Encoding.Default.GetBytes("JOIN\n");
			stream.Write(buffer, 0, 5);
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}

		try {
			byte[] buffer = new byte[128];
			int ret = stream.Read(buffer, 0, 128);
			Debug.Log ("ok " + Encoding.Default.GetString(buffer));
		} catch {
			Debug.Log ("failed recieving response");
		}
	}

	public void onClickDo () {
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return;
		}

		// 发送用户操作给服务器，由服务器转发给对手客户端
		Debug.Log ("Sending MOVE to server");
		try {
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}
	}

	public void onClickReady () {
		if (client == null || !client.Connected) {
			Debug.Log ("server not connected");
			return;
		}
		Debug.Log ("Send READY to server when ready for next turn");
		try {
			Debug.Log ("ok");
		} catch {
			Debug.Log ("failed");
		}
	}
}
