using System;
using UnityEngine;

namespace OnlineShooter.Network.Shared.Datagrams
{
	public interface IDatagram
	{
		public void OnFailedSent(Action e);
		public byte[] ToArray();
	}
}