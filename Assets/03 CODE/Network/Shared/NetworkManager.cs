using System;
using OnlineShooter.Network.Shared.Datagrams;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
	protected const int PORT = 50000;
	protected const string HOSTNAME = "127.0.0.1";

	protected UdpClient _receiver, _sender;

	protected async void ListenForDataAsync()
	{
		var packet = await _receiver.ReceiveAsync();
		ListenForDataAsync();

		var baseDatagram = new Datagram(packet.Buffer);
		var rawData = baseDatagram.GetData();

		if (baseDatagram.IsError)
		{
			HandleError(baseDatagram.GetDatagramType(), new Datagrams.ErrorDatagram());
			return;
		}

		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.ConnectionRequestResponse:
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.ConnectionRequestConfirmation:
				ServerData(baseDatagram, rawData);
				break;
		}
	}

	protected async void SendDataAsync(
		UdpClient udpClient,
		DatagramType datagramType,
		IDatagram data,
		byte clientID,
		IPEndPoint remoteEndPoint)
	{
		var datagram = new Datagram(datagramType, data, clientID);
		var rawDatagram = datagram.ToArray();

		if (remoteEndPoint == null)
		{
			if (!udpClient.Client.Connected)
			{
				throw new Exception("SOCKET IS NOT CONNECTED TO ANYWHERE!");
			}

			await udpClient.SendAsync(rawDatagram, rawDatagram.Length);
		}
		else
		{
			await udpClient.SendAsync(rawDatagram, rawDatagram.Length, remoteEndPoint);
		}
	}

	protected void SendDataAsync(DatagramType datagramType, IDatagram data, byte clientId, IPEndPoint remoteEndPoint)
	{
		SendDataAsync(_sender, datagramType, data, clientId, remoteEndPoint);
	}

	protected void SendDataAsync(DatagramType datagramType, IDatagram data, IPEndPoint remoteEndPoint)
	{
		SendDataAsync(_sender, datagramType, data, 0, remoteEndPoint);
	}

	protected void SendDataAsync(DatagramType datagramType, IDatagram data, byte clientId)
	{
		SendDataAsync(_sender, datagramType, data, clientId, null);
	}

	protected void SendDataAsync(DatagramType datagramType, IDatagram data)
	{
		SendDataAsync(_sender,datagramType, data,0,null);
	}

	protected virtual void HandleError(DatagramType datagramType, Datagrams.ErrorDatagram errorDatagram){}
	protected virtual void ServerData(Datagram baseDatagram, byte[] rawData){}
	protected virtual void ClientData(Datagram baseDatagram, byte[] rawData){}
}
