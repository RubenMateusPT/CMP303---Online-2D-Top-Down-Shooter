
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ServerClient : NetworkClient
{
	private IPEndPoint _remoteEndPoint;

	public IPEndPoint GetRemoteEndPoint => _remoteEndPoint;

	public bool IsCheckingIfAlive = false;

	public ServerClient(byte id, string name,string color, IPEndPoint remoteEndPoint)
		:base(id, name, color)
	{
		_remoteEndPoint = remoteEndPoint;
	}
}
