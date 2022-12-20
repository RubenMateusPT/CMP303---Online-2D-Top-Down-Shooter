using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using OnlineShooter.Network.Shared.Datagrams;
using OnlineShooter.Network.Shared.Errors;
using UnityEngine;
using UnityEngine.SceneManagement;
using static NetworkClient;
using static OnlineShooter.Network.Shared.Datagrams.Datagrams;
using Random = System.Random;

public class ClientNetworkManager : NetworkManager
{
	public enum NetworkStatusCode
	{
		Connecting,
		NoResponseFromServer,
		ServerIsFull,
		SuccessfullConnection,
		RequestingGameData,
		FailedToGetGameData,
		SuccesfullyGotGameData,
		PlayerConfirmationFailed
	}
	//events
	public Action<NetworkStatusCode> NetworkStatus;

	private NetworkClient _locaClient;
	private Dictionary<byte, NetworkClient> _clients;

	private GameManager _gameManager;

	//FLAGS
	private bool _confirmedByServer = false;
	private float _checkServerTimer = 0;
	private bool _isCheckingServer = false;
	private int _statusCounter = 0;
	private bool _isDisconneting = false;
	private bool _finishedDisconnecting = false;
	private bool _firstServerTickUpdate = true;

	private void Start()
	{
		_statusCounter = 0;
	}

	private void Update()
	{
		_packetManager.Update(Time.deltaTime);

		if (_statusCounter != 5)
			return;

		CheckIfAlive();
	}

	private void CheckIfAlive()
	{
		if (_isCheckingServer)
		{
			return;
		}

		_checkServerTimer += Time.deltaTime;

		if (_checkServerTimer > 5)
		{

			_checkServerTimer = 0;
			_isCheckingServer = true;
			
			SendDataAsync(
				DatagramType.AreYouAlive,
				new Datagrams.AreYouAliveDatagram()
				{
					OnFailAction = () =>
					{
						if (!_isConnected)
							return;

						Debug.LogWarning("Disconnected From Server");

						if (_statusCounter <= 0)
							return;
						Disconnect();
					}
				},
				_locaClient.GetId,
				true
			);
		}
	}

	protected override void HandleError(Datagrams.ErrorDatagram errorDatagram)
	{
		switch (errorDatagram.Error)
		{
			case NetworkError.FullServer:
				NetworkStatus.Invoke(NetworkStatusCode.ServerIsFull);
				break;
			case NetworkError.AlreadyConnecting:
				Debug.Log(errorDatagram.ErrorMessage);
				break;
		}
	}

	protected override void ClientData(Datagram baseDatagram, byte[] rawData)
	{
		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.ConnectionRequestResponse:	
				ProcessConnectionRequestResponse(
					baseDatagram, 
					new Datagrams.ConnectionRequestResponseDatagram(rawData));
				break;

			case DatagramType.GameDataResponse:
				ProcessGameData(baseDatagram, new Datagrams.GameDataDatagram(rawData));
				break;

			case DatagramType.NewPlayerGroupRequest:
				ProcessNewPlayer(baseDatagram, new NewPlayerGroupRequest(rawData));
				break;

			case DatagramType.NewPlayerJoinResponse:
				FinishPlayerSetup(baseDatagram, new Datagrams.NewPlayerJoinResponse(rawData));
				break;

			case DatagramType.AreYouAlive:
				SendDataAsync(
					DatagramType.AreYouAliveResponse,
					new AcknowledgeDatagram
					{
						RequestPacketGUID = baseDatagram.GetPacketID
					},
					_locaClient.GetId,
					true);
				break;

			case DatagramType.AreYouAliveResponse:
				_packetManager.ReceivedPacket(new AcknowledgeDatagram(rawData).RequestPacketGUID);
				_isCheckingServer = false;
				break;

			case DatagramType.RemoveClient:
				RemoveClient(baseDatagram.GetPacketID, baseDatagram.GetClientID);
				break;

			case DatagramType.DisconnectRequest:
				Disconnect();
				break;

			case DatagramType.DisconnectRequestResponse:
				_packetManager.ReceivedPacket(new AcknowledgeDatagram(rawData).RequestPacketGUID);
				SendDataAsync(
					DatagramType.Acknowledge, 
					new AcknowledgeDatagram
				{
					RequestPacketGUID = baseDatagram.GetPacketID
				},
					_locaClient.GetId);
				_finishedDisconnecting = true;
				break;

			case DatagramType.PlayerMovement:
				UpdatePlayerMovement(baseDatagram, new PlayerMovement(rawData));
				break;

		}
	}

	private void ConnectToServer()
	{
		if (_statusCounter != 0)
			return;
		_statusCounter = 1;
		
		_receiver.Connect(_hostname, _port);
		_sender.Connect(_hostname, _port);

		NetworkStatus.Invoke(NetworkStatusCode.Connecting);

		_locaClient = 
			new NetworkClient(0, PlayerSettings.Instance.Username, PlayerSettings.Instance.PlayerColor);

		Debug.Log("Requesting Connection To Server");
		SendDataAsync(
			DatagramType.ConnectionRequest,
			new Datagrams.ConnectionRequestDatagram
			{
				PlayerName = PlayerSettings.Instance.Username,
				PlayerColor = PlayerSettings.Instance.PlayerColor,
				Receiver = (IPEndPoint)_receiver.Client.LocalEndPoint,

				OnFailAction = () =>
				{
					NetworkStatus.Invoke(NetworkStatusCode.NoResponseFromServer);
					_isConnected = false;
				}
			},
			true
		);
	}

	private void ProcessConnectionRequestResponse(Datagram baseDatagram, Datagrams.ConnectionRequestResponseDatagram data)
	{
		if (_statusCounter != 1)
			return;
		_statusCounter = 2;
		Debug.Log("Received OK to join from Server");
		_packetManager.ReceivedPacket(data.RequestPacketGUID);

		NetworkStatus.Invoke(NetworkStatusCode.SuccessfullConnection);
		if (_locaClient.GetId != 0)
			return;

		_locaClient._id = baseDatagram.GetClientID;
		Debug.Log($"Received ID: {_locaClient.GetId}");
		
		_sender.Connect(_hostname, data.ReceiverPort);
		_receiver.Connect(_hostname, data.SenderPort);

		Debug.Log("Requesting Game Data To Server");
		NetworkStatus.Invoke(NetworkStatusCode.RequestingGameData);
		SendDataAsync(
			DatagramType.GameDataRequest,
			new Datagrams.RequestGameDataDatagram
			{
				RequestPacketGUID = baseDatagram.GetPacketID,
				OnFailAction = () =>
				{
					NetworkStatus.Invoke(NetworkStatusCode.FailedToGetGameData);
					_isConnected = false;
				}
			},
			_locaClient.GetId,
			true
			);
	}

	private void ProcessGameData(Datagram baseDatagram, Datagrams.GameDataDatagram gameDataDatagram)
	{
		if (_statusCounter != 2)
			return;
		_statusCounter = 3;
		Debug.Log("Received Game Data from Server");
		_packetManager.ReceivedPacket(gameDataDatagram.RequestPacketGUID);

		if (_clients != null)
			return;

		NetworkStatus.Invoke(NetworkStatusCode.SuccesfullyGotGameData);

		_clients = new Dictionary<byte, NetworkClient>();
		foreach (var serializableNetworkClient in gameDataDatagram.Players)
		{
			var ns = SerializableNetworkClient.ConvertToOriginal(serializableNetworkClient);
			_clients.Add(ns.GetId,ns);
		}

		Debug.Log("Requesting confirmation of myself from Server");
		SendDataAsync(
			DatagramType.NewPlayerJoin, 
			new Datagrams.NewPlayerJoin
			{
				RequestPacketGUID = baseDatagram.GetPacketID,
				OnFailAction = () =>
				{
					NetworkStatus.Invoke(NetworkStatusCode.PlayerConfirmationFailed);
					_isConnected = false;
				}
			},
			_locaClient.GetId,
			true
			);
	}

	private void ProcessNewPlayer(Datagram baseDatagram, Datagrams.NewPlayerGroupRequest gameDataDatagram)
	{
		if (_statusCounter != 5)
			return;
		Debug.Log($"Received player {gameDataDatagram.Client.Name}");

		var newClient = SerializableNetworkClient.ConvertToOriginal(gameDataDatagram.Client);
		if (!_clients.ContainsKey(newClient.GetId))
		{
			_clients.Add(newClient.GetId, newClient);
		}

		_gameManager.CreatePlayer(newClient, false);

		SendDataAsync(DatagramType.NewPlayerGroupResponse,
			new NewPlayerGroupResponse
			{
				RequestPacketGUID = baseDatagram.GetPacketID
			},
			_locaClient.GetId,
			true
			);
	}

	private void FinishPlayerSetup(Datagram baseDatagram, Datagrams.NewPlayerJoinResponse gameDataDatagram)
	{
		if (_statusCounter != 3)
			return;
		_statusCounter = 4;

		Debug.Log("Received OK from to server to load game");
		_packetManager.ReceivedPacket(gameDataDatagram.RequestPacketGUID);

		if (_firstServerTickUpdate)
		{
			_firstServerTickUpdate = true;
			var currentTime = DateTime.Now;
			float timeDiff = (float)baseDatagram.GetTimeStamp.Subtract(currentTime).TotalSeconds;
			float tickOffset = timeDiff / 0.1f;
			Ticks = baseDatagram.Ticks + (int)tickOffset;
		}

		if (_confirmedByServer)
			return;

		if (_clients.ContainsKey(_locaClient.GetId))
			return;

		_clients.Add(_locaClient.GetId, _locaClient);
		_confirmedByServer = true;

		var sceneLoad = SceneManager.LoadSceneAsync("Game");
		if(sceneLoad.progress > 0)
			return;

		StartCoroutine(LoadGameScene(sceneLoad));
	}

	private IEnumerator LoadGameScene(AsyncOperation sceneOperation)
	{
		while (!sceneOperation.isDone)
			yield return new WaitForEndOfFrame();

		while (!_confirmedByServer)
			yield return new WaitForEndOfFrame();

		_statusCounter = 5;

		_gameManager = GameObject.FindObjectOfType<GameManager>();

		foreach (var networkClient in _clients.Values.OrderBy(c => c.GetId))
		{
			_gameManager.CreatePlayer(networkClient, networkClient.GetId == _locaClient.GetId);
		}

	}

	private void RemoveClient(Guid baseDatagramGetPacketID, byte baseDatagramGetClientID)
	{
		var c = _clients.Values.FirstOrDefault(c => c.GetId == baseDatagramGetClientID);

		if (c == null)
			return;

		_clients.Remove(c.GetId);
		_gameManager.DeletePlayer(c);

		SendDataAsync(
			DatagramType.Acknowledge, 
			new AcknowledgeDatagram
		{
			RequestPacketGUID = baseDatagramGetPacketID
		});
	}

	public void ConnectToServer(string hostname, int port)
	{
		_statusCounter = 0;
		_hostname = hostname;
		_port = port;

		_finishedDisconnecting = false;
		_isConnected = true;
		_receiver = new UdpClient();
		_sender = new UdpClient();
		_confirmedByServer = false;
		_clients = null;
		_isDisconneting = false;
		_firstServerTickUpdate = true;

		ListenForDataAsync();
		ConnectToServer();
	}

	public void SendPlayerMovement(Vector2 pos, float angle, int ticks)
	{
		SendDataAsync(
			DatagramType.PlayerMovement,
			new PlayerMovement
			{
				Pos =  pos,
				Angle = angle,
				PlayerGameTick = Ticks,
				PlayerTicks = ticks
			},
			_locaClient.GetId
			);
	}

	private void UpdatePlayerMovement(Datagram baseDatagram, PlayerMovement rawData)
	{
		if (!_clients.ContainsKey(baseDatagram.GetClientID))
			return;

		if (baseDatagram.GetClientID == _locaClient.GetId)
			return;

		if (_clients[baseDatagram.GetClientID] == null)
			return;

		if (_clients[baseDatagram.GetClientID].PlayerGO == null)
			return;

		_clients[baseDatagram.GetClientID].PlayerGO.UpdatePosition(baseDatagram,rawData);
	}

	public void Disconnect(bool tellServer = false)
	{
		if(_isDisconneting)
			return;

		_isDisconneting = true;
		StartCoroutine(CloseConnection());

		if (_sender != null)
		{
			Debug.Log("Disconnecting from server...");
			if (tellServer)
			{
				SendDataAsync(DatagramType.DisconnectRequest,
					new DisconnectRequest
					{
						OnFailAction = () =>
						{
							_finishedDisconnecting = true;
						}
					},
					_locaClient.GetId,
					true
				);
				return;
			}
		}

		_finishedDisconnecting = true;
	}

	private void OnApplicationQuit()
	{
		Disconnect(true);
	}

	private IEnumerator CloseConnection()
	{
		while (!_finishedDisconnecting)
		{
			yield return new WaitForEndOfFrame();
		}

		_packetManager.ClearAllPackets();

		_isConnected = false;

		if (_sender != null)
		{
			_receiver.Close();
			_sender.Close();
		}

		_isCheckingServer = false;
		_statusCounter = 0;

		while (_packetManager.IsWorking)
		{
			yield return new WaitForEndOfFrame();
		}

		SceneManager.LoadScene("Main Menu");
	}
}
