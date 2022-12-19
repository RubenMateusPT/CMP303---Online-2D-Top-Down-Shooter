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

	private void Start()
	{
		_receiver = new UdpClient();
		_sender = new UdpClient();
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
				ProcessConnectionRequestResponse(
					baseDatagram, 
					new Datagrams.ConnectionRequestResponseDatagram(rawData));
				break;
		}
	}

	private void ConnectToServer()
	{
		_receiver.Connect(_hostname, _port);
		_sender.Connect(_hostname, _port);

		Random ran = new Random();

		SendDataAsync(
			DatagramType.ConnectionRequest,
			new Datagrams.ConnectionRequestDatagram
			{
				PlayerName = PlayerSettings.Instance.Username,
				PlayerColor = PlayerSettings.Instance.PlayerColor,
				Receiver = (IPEndPoint)_receiver.Client.LocalEndPoint
			},
			true
		);
	}

	private void ProcessConnectionRequestResponse(Datagram baseDatagram, Datagrams.ConnectionRequestResponseDatagram data)
	{
		_packetManager.ReceivedPacket(data.RequestPacketGUID);

		id = baseDatagram.GetClientID;
		Debug.Log($"Received ID: {id}");
		
		_sender.Connect(_hostname, data.ReceiverPort);
		_receiver.Connect(_hostname, data.ReceiverPort);

		SendDataAsync(
			DatagramType.Acknowledge, 
			new Datagrams.AcknowledgeDatagram
			{
				RequestPacketGUID = baseDatagram.GetPacketID
			},
			true);
	}



	public void ConnectToServer(string hostname, int port)
	{
		_hostname = hostname;
		_port = port;
		
		ConnectToServer();
	}

}
