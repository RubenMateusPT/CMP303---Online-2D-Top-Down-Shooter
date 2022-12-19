
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ServerClient : NetworkClient
{
	private IPEndPoint _remoteEndPoint;

	public IPEndPoint GetRemoteEndPoint => _remoteEndPoint;

	public ServerClient(byte id, string name, IPEndPoint remoteEndPoint)
		:base(id, name)
	{
		_remoteEndPoint = remoteEndPoint;
	}
}
