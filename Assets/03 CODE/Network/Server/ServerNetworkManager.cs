using OnlineShooter.Network.Shared.Datagrams;
using OnlineShooter.Network.Shared.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using static OnlineShooter.Network.Shared.Datagrams.Datagrams;
using Random = UnityEngine.Random;

public class ServerNetworkManager : NetworkManager
{

	private const int MAX_PLAYERS = 4;

	private UdpClient _listener;

	private byte _idCounter = 0;
	private Dictionary<byte, ServerClient> _joiningClients = new Dictionary<byte, ServerClient>();
	private Dictionary<byte, ServerClient> _clients = new Dictionary<byte, ServerClient>();
	private Queue<byte> _disconnectedClients = new Queue<byte>();

	private Dictionary<byte, bool> _groupResponse = new Dictionary<byte, bool>();
	private Tuple<ServerClient, Guid> _candidate;

	private float _aliveTimer = 0;
	private int _statusCounter = 0;

	private void Start()
	{
		_listener = new UdpClient(_port);
		//_listener.Client.SendTimeout = 1000;

		Debug.Log($"Created UDP Listening Socket at Port {_port}");

		int randomPort = _port + Random.Range(1, 50);
		_receiver = new UdpClient(randomPort);
		_sender = new UdpClient(randomPort + 1);
		Debug.Log($"Created UDP Receiver Socket at {randomPort}");
		Debug.Log($"Created UDP Sender Socket at {randomPort + 1}");

		Debug.Log("Server is ready");
		_isConnected = true;
		ListenForNewClients();
		ListenForDataAsync();
	}

	private void Update()
	{
		ListenForNewClients();

		_packetManager.Update(Time.deltaTime);

		if (_clients.Count <= 0)
			return;

		CheckIfAlive();
	}

	private void CheckIfAlive()
	{
		if (!_isConnected)
		{
			return;
		}

		_aliveTimer += Time.deltaTime;

		if (_aliveTimer >= 5)//Every 5 seconds
		{
			_aliveTimer = 0;

			while (_disconnectedClients.Count > 0)
			{
				byte toRemoveId = _disconnectedClients.Dequeue();

				if (!_clients.ContainsKey(toRemoveId))
					continue;

				ServerClient clientToRemove;
				_clients.Remove(toRemoveId, out clientToRemove);

				if (_clients.Count <= 0 && _candidate != null)
				{
					_statusCounter = 4;
					AcceptCandidate();
				}

				foreach (var serverClient in _clients.Values)
				{
					if (!_clients.ContainsKey(serverClient.GetId))
						continue;

					SendDataAsync(
						DatagramType.RemoveClient,
						new Datagrams.RemoveClientDatagram
						{
							OnFailAction = () =>
							{
								if (!_clients.ContainsKey(serverClient.GetId))
									return;
								_disconnectedClients.Enqueue(serverClient.GetId);
							}
						},
						clientToRemove.GetId,
						serverClient.GetRemoteEndPoint,
						true
					);
				}

			}

			foreach (var serverClient in _clients.Values)
			{
				if (serverClient.IsCheckingIfAlive)
					return;

				serverClient.IsCheckingIfAlive = true;

				Debug.LogWarning($"Checking if {serverClient.GetName} is alive");
				SendDataAsync(
					DatagramType.AreYouAlive,
					new Datagrams.AreYouAliveDatagram()
					{
						OnFailAction = () =>
						{
							if (!_clients.ContainsKey(serverClient.GetId))
								return;
							_disconnectedClients.Enqueue(serverClient.GetId);
						}
					},
					serverClient.GetRemoteEndPoint,
					true
					);
			}
		}
	}

	private async void ListenForNewClients()
	{
		if (!_isConnected)
			return;

		_statusCounter = 0;
		Debug.Log("Listening for new clients...");
		var packet = await _listener.ReceiveAsync();

		Debug.Log($"New client has connected from {packet.RemoteEndPoint}");
		var baseDatagram = new Datagram(packet.Buffer);
		var rawData = baseDatagram.GetData();

		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.ConnectionRequest:
				ProcessNewClient(baseDatagram, new Datagrams.ConnectionRequestDatagram(rawData));
				break;
		}
	}

	private void ProcessNewClient(Datagram datagram, Datagrams.ConnectionRequestDatagram data)
	{
		if (_statusCounter != 0)
			return;
		_statusCounter = 1;
		Debug.Log("Received new join request");
		_idCounter++;

		var newClient = new ServerClient(_idCounter, data.PlayerName, data.PlayerColor, data.Receiver);

		if (_clients.Count >= MAX_PLAYERS)
		{
			SendDataAsync(
				_listener,
				DatagramType.Error,
				new Datagrams.ErrorDatagram
				{
					Error = NetworkError.FullServer,
					ErrorMessage = "Server is Full",
					RequestPacketGUID = datagram.GetPacketID
				},
				0,
				data.Receiver);

			_idCounter--;
			ListenForNewClients();
			return;
		}

		if (_joiningClients.Values.Any(c => c.GetRemoteEndPoint == data.Receiver))
		{
			SendDataAsync(
				_listener,
				DatagramType.Error,
				new Datagrams.ErrorDatagram
				{
					Error = NetworkError.AlreadyConnecting,
					ErrorMessage = "You are already attempting to connect!",
					RequestPacketGUID = datagram.GetPacketID
				},
				0,
				data.Receiver);

			_idCounter--;
			ListenForNewClients();
			return;
		}

		Debug.Log($"Received Player: {data.PlayerName}");

		_joiningClients.Add(_idCounter, newClient);

		Debug.Log("Sending join confirmation");
		SendDataAsync(
			_listener,
			DatagramType.ConnectionRequestResponse,
			new Datagrams.ConnectionRequestResponseDatagram
			{
				RequestPacketGUID = datagram.GetPacketID,
				ReceiverPort = ((IPEndPoint)_receiver.Client.LocalEndPoint).Port,
				SenderPort = ((IPEndPoint)_sender.Client.LocalEndPoint).Port,
				OnFailAction = () =>
				{
					_joiningClients.Remove(_idCounter);
					_idCounter--;
					ListenForNewClients();
				}
			},
			_idCounter,
			newClient.GetRemoteEndPoint,
			true
		);
	}

	protected override void ServerData(Datagram baseDatagram, byte[] rawData)
	{
		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.GameDataRequest:
				SendCurrentGameData(baseDatagram, new Datagrams.RequestGameDataDatagram(rawData));
				break;

			case DatagramType.NewPlayerJoin:
				AddNewPlayer(baseDatagram, new Datagrams.NewPlayerJoin(rawData));
				break;

			case DatagramType.NewPlayerGroupResponse:
				GroupAsAcceptedPlayer(baseDatagram, new Datagrams.NewPlayerGroupResponse(rawData));
				break;

			case DatagramType.AreYouAlive:
				SendDataAsync(
					DatagramType.AreYouAliveResponse,
					new AcknowledgeDatagram
					{
						RequestPacketGUID = baseDatagram.GetPacketID
					},
					_clients[baseDatagram.GetClientID].GetRemoteEndPoint,
					true
					);
				break;

			case DatagramType.AreYouAliveResponse:
				_packetManager.ReceivedPacket(new Datagrams.AcknowledgeDatagram(rawData).RequestPacketGUID);
				if (!_clients.ContainsKey(baseDatagram.GetClientID)) return;
				_clients[baseDatagram.GetClientID].IsCheckingIfAlive = false;
				break;

			case DatagramType.DisconnectRequest:
				_disconnectedClients.Enqueue(baseDatagram.GetClientID);
				SendDataAsync(
					DatagramType.DisconnectRequestResponse,
					new AcknowledgeDatagram
					{
						RequestPacketGUID = baseDatagram.GetPacketID
					},
					_clients[baseDatagram.GetClientID].GetRemoteEndPoint,
					true
				);
				break;

			case DatagramType.PlayerMovement:
				UpdatePlayerMovement(baseDatagram, new Datagrams.PlayerMovement(rawData));
				break;

		}
	}

	private void SendCurrentGameData(Datagram baseDatagram, Datagrams.RequestGameDataDatagram data)
	{
		if (_statusCounter != 1)
			return;
		_statusCounter = 2;
		Debug.Log("Received request for current game data");
		_packetManager.ReceivedPacket(data.RequestPacketGUID);

		Debug.Log("Sending current game data");
		SendDataAsync(
			DatagramType.GameDataResponse,
			new Datagrams.GameDataDatagram
			{
				RequestPacketGUID = baseDatagram.GetPacketID,
				Players = _clients.Values.ToList().ConvertAll(sc => new NetworkClient.SerializableNetworkClient(sc)),
				OnFailAction = () =>
				{
					_joiningClients.Remove(_idCounter);
					_idCounter--;
					ListenForNewClients();
				}
			},
			_joiningClients[baseDatagram.GetClientID].GetRemoteEndPoint,
			true
			);
	}

	private void AddNewPlayer(Datagram baseDatagram, Datagrams.NewPlayerJoin data)
	{
		if (_statusCounter != 2)
			return;
		_statusCounter = 3;
		Debug.Log("Received player confirmation to load game");
		_packetManager.ReceivedPacket(data.RequestPacketGUID);


		if (_candidate != null)
		{
			return;
		}

		_candidate = new Tuple<ServerClient, Guid>(_joiningClients[baseDatagram.GetClientID], baseDatagram.GetPacketID);
		_joiningClients.Remove(baseDatagram.GetClientID);

		if (_clients.Count > 0)
		{
			Debug.Log("Sending new player to everyone");
			_groupResponse.Clear();
			foreach (var serverClient in _clients.Values)
			{
				_groupResponse.Add(serverClient.GetId, false);
				SendDataAsync(
					DatagramType.NewPlayerGroupRequest,
					new Datagrams.NewPlayerGroupRequest
					{
						Client = new NetworkClient.SerializableNetworkClient(_candidate.Item1),
						OnFailAction = () =>
						{
							if (_candidate != null)
							{
								_idCounter--;
							}
							_candidate = null;
							ListenForNewClients();
						}
					},
					serverClient.GetRemoteEndPoint,
					true
				);
			}
		}
		else
		{
			_statusCounter = 4;
			AcceptCandidate();
		}
	}

	private void GroupAsAcceptedPlayer(Datagram baseDatagram, Datagrams.NewPlayerGroupResponse data)
	{
		if (_statusCounter != 3)
			return;
		_statusCounter = 4;
		Debug.Log($"Received group confirmation to accept player from {_clients[baseDatagram.GetClientID].GetName}");
		_packetManager.ReceivedPacket(data.RequestPacketGUID);

		if (_groupResponse[baseDatagram.GetClientID])
			return;

		_groupResponse[baseDatagram.GetClientID] = true;

		if (_groupResponse.Values.Any(r => !r))
			return;

		if (_candidate == null)
			return;

		AcceptCandidate();
	}

	private void AcceptCandidate()
	{
		if (_statusCounter != 4)
			return;
		_statusCounter = 5;

		_clients.Add(_candidate.Item1.GetId, _candidate.Item1);
		Debug.Log("Sent confirmation to load game");
		SendDataAsync(
			DatagramType.NewPlayerJoinResponse,
			new Datagrams.NewPlayerJoinResponse
			{
				RequestPacketGUID = _candidate.Item2,
			},
			_candidate.Item1.GetRemoteEndPoint,
			true
		);

		_candidate = null;
		ListenForNewClients();
	}

	private void UpdatePlayerMovement(Datagram baseDatagram, Datagrams.PlayerMovement playerMovement)
	{
		foreach (var serverClient in _clients.Values)
		{
			SendDataAsync(DatagramType.PlayerMovement,
				playerMovement,
				baseDatagram.GetClientID,
				serverClient.GetRemoteEndPoint
				);
		}
	}

	private void OnApplicationQuit()
	{
		_isConnected = false;

		foreach (var serverClient in _clients.Values)
		{
			SendDataAsync(
				DatagramType.DisconnectRequest,
				new Datagrams.EmptyDatagram()
				{
				},
				serverClient.GetRemoteEndPoint
			);
		}

		_listener.Close();
		_receiver.Close();
		_sender.Close();
	}
}
