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
using Random = UnityEngine.Random;

public class ServerNetworkManager : NetworkManager
{
	private const int MAX_PLAYERS = 4;

	private UdpClient _listener;

	private byte _idCounter = 0;
	private List<ServerClient> _joiningClients = new List<ServerClient>();
	private List<ServerClient> _clients = new List<ServerClient>();

	private void Start()
	{
		_listener = new UdpClient(_port);
		//_listener.Client.SendTimeout = 1000;

		Debug.Log($"Created UDP Listening Socket at Port {_port}");

		int randomPort = _port + Random.Range(1,50);
		_receiver = new UdpClient(randomPort);
		_sender = new UdpClient(randomPort + 1);
		Debug.Log($"Created UDP Receiver Socket at {randomPort}");
		Debug.Log($"Created UDP Sender Socket at {randomPort + 1}");

		Debug.Log("Server is ready");
		ListenForNewClients();
		ListenForDataAsync();
	}

	private async void ListenForNewClients()
	{
		Debug.Log("Listening for new clients...");
		var packet = await _listener.ReceiveAsync();
		ListenForNewClients();

		Debug.Log($"New client has connected from {packet.RemoteEndPoint}");
		var baseDatagram = new Datagram(packet.Buffer);
		var rawData = baseDatagram.GetData();

		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.ConnectionRequest:
				ProcessNewClient(baseDatagram,new Datagrams.ConnectionRequestDatagram(rawData));
				break;
		}
	}

	private void ProcessNewClient(Datagram datagram, Datagrams.ConnectionRequestDatagram data)
	{
		_idCounter++;

		Debug.Log($"Received Player: {data.PlayerName}");
		var newClient = new ServerClient(_idCounter, data.PlayerName, data.PlayerColor, data.Receiver);
		_joiningClients.Add(newClient);

		SendDataAsync(
			_listener, 
			DatagramType.ConnectionRequestResponse,
			new Datagrams.ConnectionRequestResponseDatagram
			{
				RequestPacketGUID = datagram.GetPacketID,
				ReceiverPort = ((IPEndPoint)_receiver.Client.LocalEndPoint).Port,
				SenderPort = ((IPEndPoint)_sender.Client.LocalEndPoint).Port,
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
			case DatagramType.ConnectionRequestConfirmation:

				Debug.Log("Client is connected!");

				break;
		}
	}
}
