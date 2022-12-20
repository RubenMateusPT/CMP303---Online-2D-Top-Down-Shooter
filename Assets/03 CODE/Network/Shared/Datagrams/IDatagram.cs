using System;
using UnityEngine;

namespace OnlineShooter.Network.Shared.Datagrams
{

	public interface IDatagram
	{
		public NetworkPacketManager.PacketSettings GetPacketSettings();
		public void OnFailedSent();
		public byte[] ToArray();
	}
}