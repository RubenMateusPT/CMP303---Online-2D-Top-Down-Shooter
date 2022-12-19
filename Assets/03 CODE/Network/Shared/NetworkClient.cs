using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkClient
{
	protected byte _id;

	protected string _name;

	public NetworkClient(byte id, string name)
	{
		_id = id;
		_name = name;
	}
}
