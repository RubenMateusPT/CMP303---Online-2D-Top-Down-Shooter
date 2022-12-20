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
	protected bool _isConnected;

	private int _ticks = 0;

	public static T GetInstance<T>()
	{
		return (T)(object)_instance;
	}

	public int Ticks
	{
		get;
		set;
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
		_isConnected = false;

		StartCoroutine(CountTick());
	}

	private IEnumerator CountTick()
	{
		while (true)
		{
			yield return new WaitForSeconds(0.1f);
			Ticks++;
		}
	}

	protected async void ListenForDataAsync()
	{
		if(!_isConnected || _receiver.Client == null)
			return;

		Debug.Log("Listening for data...");
		UdpReceiveResult packet;

		try
		{
			packet = await _receiver.ReceiveAsync();
		}
		catch (ObjectDisposedException ex)
		{
			return;
		}

		ListenForDataAsync();

		var baseDatagram = new Datagram(packet.Buffer);
		var rawData = baseDatagram.GetData();

		if (baseDatagram.IsError)
		{
			var error = new Datagrams.ErrorDatagram(rawData);
			_packetManager.ReceivedPacket(error.RequestPacketGUID);
			HandleError(error);
			return;
		}

		switch (baseDatagram.GetDatagramType())
		{
			case DatagramType.Acknowledge:
				_packetManager.ReceivedPacket(
					new Datagrams.AcknowledgeDatagram(rawData).RequestPacketGUID);
				break;

			case DatagramType.ConnectionRequestResponse:
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.GameDataRequest:
				ServerData(baseDatagram, rawData);
				break;

			case DatagramType.GameDataResponse:
				ClientData(baseDatagram,rawData);
				break;

			case DatagramType.NewPlayerJoin:
				ServerData(baseDatagram, rawData);
				break;

			case DatagramType.NewPlayerJoinResponse:
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.NewPlayerGroupRequest:
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.NewPlayerGroupResponse:
				ServerData(baseDatagram, rawData);
				break;

			case DatagramType.AreYouAlive:
				ServerData(baseDatagram,rawData);
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.AreYouAliveResponse:
				ServerData(baseDatagram, rawData);
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.DisconnectRequest:
				ServerData(baseDatagram,rawData);
				ClientData(baseDatagram,rawData);
				break;

			case DatagramType.DisconnectRequestResponse:
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.RemoveClient:
				ClientData(baseDatagram, rawData);
				break;

			case DatagramType.PlayerMovement:
				ServerData(baseDatagram,rawData);
				ClientData(baseDatagram,rawData);
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
				Data = new Datagram(_ticks, datagramType, data, clientID, datagramType == DatagramType.Error),
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

	protected virtual void HandleError(Datagrams.ErrorDatagram errorDatagram){}
	protected virtual void ServerData(Datagram baseDatagram, byte[] rawData){}
	protected virtual void ClientData(Datagram baseDatagram, byte[] rawData){}
}
