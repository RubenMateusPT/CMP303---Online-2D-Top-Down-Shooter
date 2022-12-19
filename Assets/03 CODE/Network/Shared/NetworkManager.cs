using System;
using OnlineShooter.Network.Shared.Datagrams;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;
using System.Data;
using Unity.Burst.Intrinsics;

public class NetworkManager : MonoBehaviour
{
	private static NetworkManager _instance;
	protected NetworkPacketManager _packetManager;

	protected int _port = 50000;
	protected string _hostname = "127.0.0.1";

	protected UdpClient _receiver, _sender;

	public static T GetInstance<T>()
	{
		return (T)(object)_instance;
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
		}
		else
		{
			_instance = this;
		}

		DontDestroyOnLoad(gameObject);

		_packetManager = new NetworkPacketManager();
	}

	private void Update()
	{
		_packetManager.Update(Time.deltaTime);
	}

	protected async void ListenForDataAsync()
	{
		Debug.Log("Listening for data...");
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
			case DatagramType.Acknowledge:
				_packetManager.ReceivedPacket(
					new Datagrams.AcknowledgeDatagram(rawData).RequestPacketGUID);
				break;

			case DatagramType.ConnectionRequestConfirmation:
				ServerData(baseDatagram, rawData);
				break;

			case DatagramType.ConnectionRequestResponse:
				ClientData(baseDatagram, rawData);
				break;
		}
	}

	protected async void SendDataAsync(
		UdpClient udpClient,
		DatagramType datagramType,
		IDatagram data,
		byte clientID,
		IPEndPoint remoteEndPoint,
		bool needsConfimation = false)
	{
		_packetManager.SendPacket(
			new NetworkPacketManager.Packet
			{
				Socket = udpClient,
				Destination = remoteEndPoint,
				Data = new Datagram(datagramType, data, clientID),
			},
			needsConfimation
			);
	}

	protected void SendDataAsync(
		DatagramType datagramType, 
		IDatagram data, 
		byte clientId, 
		IPEndPoint remoteEndPoint,
		bool needsConfimation = false)
	{
		SendDataAsync(_sender, datagramType, data, clientId, remoteEndPoint, needsConfimation);
	}

	protected void SendDataAsync(
		DatagramType datagramType, 
		IDatagram data, 
		IPEndPoint remoteEndPoint,
		bool needsConfimation = false)
	{
		SendDataAsync(_sender, datagramType, data, 0, remoteEndPoint, needsConfimation);
	}

	protected void SendDataAsync(
		DatagramType datagramType, 
		IDatagram data, 
		byte clientId,
		bool needsConfimation = false)
	{
		SendDataAsync(_sender, datagramType, data, clientId, null, needsConfimation);
	}

	protected void SendDataAsync(
		DatagramType datagramType, 
		IDatagram data,
		bool needsConfimation = false)
	{
		SendDataAsync(_sender,datagramType, data,0,null,needsConfimation);
	}

	protected virtual void HandleError(DatagramType datagramType, Datagrams.ErrorDatagram errorDatagram){}
	protected virtual void ServerData(Datagram baseDatagram, byte[] rawData){}
	protected virtual void ClientData(Datagram baseDatagram, byte[] rawData){}
}
