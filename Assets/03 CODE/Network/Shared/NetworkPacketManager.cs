using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading.Tasks;
using OnlineShooter.Network.Shared.Datagrams;
using UnityEngine;

public class NetworkPacketManager
{
	private const byte MAX_SEND_RETRIES = 3;
	private const float TIME_TO_RESEND_PACKET = 5; //In Seconds

	private float _timer = 0;

	private List<SpecialPacket> _packetsNeedingConfirmation = new List<SpecialPacket>();

	private async Task SendPacketAsync(Packet packet, bool isImportant = true)
	{
		string importantText = packet.Data.GetDatagramType() == DatagramType.Acknowledge ? "ACK" : isImportant ? "IMPORTANT" : "";
		Debug.Log($"Sent {importantText} packet: {packet.Data.GetPacketID}");
		var datagram = packet.Data.ToArray();

		if (packet.Destination == null)
		{
			await packet.Socket.SendAsync(datagram, datagram.Length);
		}
		else
		{
			await packet.Socket.SendAsync(datagram, datagram.Length, packet.Destination);
		}
	}

	private async void SendPacketAsync(SpecialPacket specialPacket, bool countAsFail = true)
	{
		if (specialPacket.Status.IsSending)
			return;

		if (specialPacket.Status.Retries >= MAX_SEND_RETRIES)
		{
			Debug.Log($"Failed to send packet: {specialPacket.Packet.Data.GetPacketID}");
			specialPacket.Status.IsResponded = true;
			return;
		}

		specialPacket.Status.IsSending = true;

		await SendPacketAsync(specialPacket.Packet);

		specialPacket.Status.IsSending = false;
		specialPacket.Status.Retries++;
	}

	private void ResendPackets()
	{
		_packetsNeedingConfirmation.RemoveAll(p => p.Status.IsResponded);

		foreach (var specialPacket in _packetsNeedingConfirmation)
		{
			SendPacketAsync(specialPacket);
		}
	}

	public void Update(float dt)
	{
		if(_packetsNeedingConfirmation.Count <= 0) return;

		if (_timer > TIME_TO_RESEND_PACKET)
		{
			_timer = 0;

			ResendPackets();
		}

		_timer += dt;
	}

	public void SendPacket(Packet packet, bool needsConfirmation)
	{
		if (needsConfirmation)
		{
			var specialPacket = new SpecialPacket
			{
				Packet = packet,
				Status = new PacketStatus
				{
					IsResponded = false,
					IsSending = false,
					Retries = 0,
				}
			};

			_packetsNeedingConfirmation.Add(specialPacket);

			SendPacketAsync(specialPacket,false);

			return;
		}

		SendPacketAsync(packet, false);
	}

	public void ReceivedPacket(Guid packetId)
	{
		Debug.Log($"Received Confirmation of Packet: {packetId}");
		var packet = _packetsNeedingConfirmation.FirstOrDefault(p => p.Packet.Data.GetPacketID == packetId);

		if(packet == null)
			return;
		
		packet.Status.IsResponded = true;
	}

	public struct Packet
	{
		public UdpClient Socket;
		public IPEndPoint Destination;
		public Datagram Data;
	}

	private struct PacketStatus
	{
		public bool IsResponded;
		public bool IsSending;
		public byte Retries;
	}

	private class SpecialPacket
	{
		public Packet Packet;
		public PacketStatus Status;
	}
}
