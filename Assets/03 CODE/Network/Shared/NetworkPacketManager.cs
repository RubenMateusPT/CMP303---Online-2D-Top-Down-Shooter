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
	private List<SpecialPacket> _packetsNeedingConfirmation = new List<SpecialPacket>();

	public bool IsWorking => _packetsNeedingConfirmation.Count > 0;

	private async Task SendPacketAsync(Packet packet, bool isImportant = true)
	{
		string importantText = packet.Data.GetDatagramType() == DatagramType.Acknowledge ? "ACK" : isImportant ? "IMPORTANT" : "";
		Debug.Log($"Sent {importantText} packet: {packet.Data.GetPacketID}");
		var datagram = packet.Data.ToArray();

		if (packet.Socket.Client == null)
			return;

		try
		{

			if (packet.Destination == null)
			{
				await packet.Socket.SendAsync(datagram, datagram.Length);
			}
			else
			{
				await packet.Socket.SendAsync(datagram, datagram.Length, packet.Destination);
			}
		}
		catch (ObjectDisposedException ex)

		{
		}
	}

	private async void SendPacketAsync(SpecialPacket specialPacket, bool countAsFail = true)
	{
		if (specialPacket.Status.IsSending)
			return;

		if (specialPacket.Status.Retries <= 0)
		{
			Debug.Log($"Failed to send packet: {specialPacket.Packet.Data.GetPacketID}");
			specialPacket.Packet.Data.GetDatagram().OnFailedSent();
			specialPacket.Status.IsResponded = true;
			return;
		}

		if (specialPacket.Status.Retries >= specialPacket.Status.MaxRetries)
		{
			specialPacket.Status.Retries = 0;
			specialPacket.Status.TimeToTimeout *= 2;
			return;
		}

		specialPacket.Status.IsSending = true;

		await SendPacketAsync(specialPacket.Packet);

		specialPacket.Status.IsSending = false;
		specialPacket.Status.Retries++;
	}

	public void Update(float dt)
	{
		if(_packetsNeedingConfirmation.Count <= 0) return;

		_packetsNeedingConfirmation.RemoveAll(p => p.Status.IsResponded);

		foreach (var packetToResend in _packetsNeedingConfirmation.Where(p => p.Status.Timer > p.Status.TimeToTimeout))
		{
			packetToResend.Status.Timer = 0;
			SendPacketAsync(packetToResend);
		}

		_packetsNeedingConfirmation.ForEach(p => p.Status.UpdateTimer(dt));
	}

	public void SendPacket(Packet packet, bool needsConfirmation)
	{
		if (needsConfirmation)
		{
			var ps= packet.Data.GetDatagram().GetPacketSettings();
			var specialPacket = new SpecialPacket
			{
				Packet = packet,
				Status = new PacketStatus
				{
					MaxRetries = ps.MaxRetries,
					Retries = 1,

					TimeToTimeout = ps.TimeToResend,
					Timer = 0,

					IsResponded = false,
					IsSending = false
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
		if (!_packetsNeedingConfirmation.Exists(p => p.Packet.Data.GetPacketID == packetId))
			return;

		var packet = _packetsNeedingConfirmation.FirstOrDefault(p => p.Packet.Data.GetPacketID == packetId);
		Debug.Log($"Received Confirmation of Packet: {packetId}");
		packet.Status.IsResponded = true;
	}

	public void ClearAllPackets()
	{
		foreach (var specialPacket in _packetsNeedingConfirmation)
		{
			specialPacket.Status.IsResponded = true;
		}
	}

	public void CancelPacket(byte _clientId)
	{
		var sp = _packetsNeedingConfirmation.FirstOrDefault(p => p.Packet.Data.GetClientID == _clientId);
		if (sp != null)
		{
			sp.Status.IsResponded = true;
		}
	}

	public struct Packet
	{
		public UdpClient Socket;
		public IPEndPoint Destination;
		public Datagram Data;
	}

	private struct PacketStatus
	{
		public float MaxRetries;
		public byte Retries;

		public float TimeToTimeout;
		public float Timer;

		public bool IsResponded;
		public bool IsSending;

		public void UpdateTimer(float dt)
		{
			Timer += dt;
		}
	}

	private class SpecialPacket
	{
		public Packet Packet;
		public PacketStatus Status;
	}

	public struct PacketSettings
	{
		public float MaxRetries;
		public float TimeToResend;
	}
}
