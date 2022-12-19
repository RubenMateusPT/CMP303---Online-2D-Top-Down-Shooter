using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using OnlineShooter.Network.Shared.Datagrams;
using UnityEngine;
using Random = System.Random;

public class ClientNetworkManager : NetworkManager
{
	private byte id = 0;

	private void Awake()
	{
		_receiver = new UdpClient();
		_sender = new UdpClient();

		ConnectToServer();
		ListenForDataAsync();
	}

	protected override void HandleError(DatagramType datagramType, Datagrams.ErrorDatagram errorDatagram)
	{
		switch (datagramType)
		{
			case DatagramType.ConnectionRequestResponse:
				Debug.Log($"Error: {errorDatagram.ErrorMessage}");
				break;
		}
	}

	protected override void ClientData(Datagram baseDatagram, byte[] rawData)
	{
		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.ConnectionRequestResponse:
				ProcessConnectionRequestResponse(baseDatagram, new Datagrams.ConnectionRequestResponseDatagram(rawData));
				break;
		}
	}

	private void ProcessConnectionRequestResponse(Datagram baseDatagram, Datagrams.ConnectionRequestResponseDatagram data)
	{
		id = baseDatagram.GetClientID;
		Debug.Log($"Received ID: {id}");
		
		_sender.Connect(HOSTNAME, data.ReceiverPort);
		_receiver.Connect(HOSTNAME, data.SenderPort);

		SendDataAsync(DatagramType.ConnectionRequestConfirmation, new Datagrams.EmptyDatagram());
	}

	public void ConnectToServer()
	{
		_receiver.Connect(HOSTNAME, PORT);
		_sender.Connect(HOSTNAME, PORT);

		Random ran = new Random();

		SendDataAsync(
			DatagramType.ConnectionRequest,
			new Datagrams.ConnectionRequestDatagram
			{
				PlayerName = $"Player {ran.Next(100)}",
				Receiver = (IPEndPoint)_receiver.Client.LocalEndPoint
			}
		);
	}
}
