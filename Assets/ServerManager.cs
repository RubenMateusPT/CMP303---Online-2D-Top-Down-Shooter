using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using UnityEngine;

namespace Server
{
	public class ServerProperties
	{
		public int Port;
	}

	public class ServerSettings
	{
		const string FILENAME = "server-settings.txt";

		public ServerProperties Properties;

		public ServerSettings()
		{
			string filePath = $"./{FILENAME}";

			Debug.Log($"Loading server settings files at: {filePath}");

			if (!File.Exists(filePath))
			{
				Debug.LogWarning($"File not Found!");
				Debug.Log($"Creating server setting with default files at: {filePath}");

				File.Create(FILENAME).Close();

				File.WriteAllText(
					filePath,
					JsonUtility.ToJson(
						new ServerProperties
						{
							Port = 5000
						},
						true
						)
					);
			}

			Properties = JsonUtility.FromJson<ServerProperties>(File.ReadAllText(filePath));

			Debug.Log("Loaded server settings from file");
		}
	}

	public class ServerManager : MonoBehaviour
	{
		private ServerSettings _settings = new ServerSettings();
		private TcpListener _listener;

		private void Awake()
		{
			_listener = new TcpListener(IPAddress.Any, _settings.Properties.Port);
			Debug.Log($"Opening Server at Endpoint: {_listener.LocalEndpoint}");

			_listener.Start();
		}
	}
}
