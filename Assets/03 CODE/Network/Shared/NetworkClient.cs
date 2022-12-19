using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkClient
{
	protected byte _id;

	protected string _name;
	protected string _color;

	public NetworkClient(byte id, string name, string color)
	{
		_id = id;
		_name = name;
		_color = color;
	}
}
