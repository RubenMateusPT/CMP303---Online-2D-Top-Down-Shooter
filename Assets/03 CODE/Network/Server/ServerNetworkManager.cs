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
	private const int MAX_PLAYERS = 0;

	private UdpClient _listener;

	private byte _idCounter = 0;

	private void Awake()
	{
		_listener = new UdpClient(PORT);

		int randomPort = Random.Range(1, 100);
		_receiver = new UdpClient(PORT + randomPort);
		_sender = new UdpClient(PORT + randomPort + 1);

		Debug.Log("Server is ready");
		ListenForNewClients();
		ListenForDataAsync();
	}

	private async void ListenForNewClients()
	{
		var packet = await _listener.ReceiveAsync();
		ListenForNewClients();

		var baseDatagram = new Datagram(packet.Buffer);
		var rawData = baseDatagram.GetData();

		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.ConnectionRequest:
				ProcessNewClient(new Datagrams.ConnectionRequestDatagram(rawData));
				break;
		}
	}

	private void ProcessNewClient(Datagrams.ConnectionRequestDatagram data)
	{
		_idCounter++;

		var newClient = new ServerClient(_idCounter, data.PlayerName, data.Receiver);

		Debug.Log($"Received Player: {data.PlayerName}");

		SendDataAsync(
			_listener, 
			DatagramType.ConnectionRequestResponse,
			new Datagrams.ConnectionRequestResponseDatagram
			{
				ReceiverPort = ((IPEndPoint)_receiver.Client.LocalEndPoint).Port,
				SenderPort = ((IPEndPoint)_sender.Client.LocalEndPoint).Port,
			},
			_idCounter,
			newClient.GetRemoteEndPoint
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
