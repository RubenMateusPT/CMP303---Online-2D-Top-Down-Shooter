using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkClient
{
	public byte _id;
	public string _name;
	public string _color;

	public byte GetId => _id;
	public string GetName => _name;
	public string Color => _color;

	public Player PlayerGO;

	public NetworkClient(byte id, string name, string color)
	{
		_id = id;
		_name = name;
		_color = color;
	}

	[Serializable]
	public struct SerializableNetworkClient
	{
		public byte Id;
		public string Name;
		public string Color;

		public SerializableNetworkClient(NetworkClient original)
		{
			Id = original._id;
			Name = original._name;
			Color = original._color;
		}

		public static NetworkClient ConvertToOriginal(SerializableNetworkClient serializable)
		{
			return new NetworkClient(serializable.Id, serializable.Name, serializable.Color);
		}
	}
}
