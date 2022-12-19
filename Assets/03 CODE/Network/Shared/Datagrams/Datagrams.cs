using System;
using System.IO;
using System.Net;
using OnlineShooter.Network.Shared.Errors;

//referance https://www.genericgamedev.com/general/converting-between-structs-and-byte-arrays/

namespace OnlineShooter.Network.Shared.Datagrams
{
	public class Datagrams
	{
		private static IPEndPoint ParseEndPoint(string endPoint)
		{
			string[] parts = endPoint.Split(':');
			return new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
		}

		public struct ErrorDatagram : IDatagram
		{
			public NetworkError Error;
			public string ErrorMessage;

			public ErrorDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				Error = (NetworkError) reader.ReadByte();
				ErrorMessage = reader.ReadString();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write((byte)Error);
				writer.Write(ErrorMessage);

				return stream.ToArray();
			}
		}

		public struct EmptyDatagram : IDatagram
		{
			public byte[] ToArray()
			{
				return Array.Empty<byte>();
			}
		}

		public struct ConnectionRequestDatagram : IDatagram
		{
			public string PlayerName;

			public IPEndPoint Receiver;

			public ConnectionRequestDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				PlayerName = reader.ReadString();
				Receiver = ParseEndPoint(reader.ReadString());
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(PlayerName);
				writer.Write(Receiver.ToString());

				return stream.ToArray();
			}
		}

		public struct ConnectionRequestResponseDatagram : IDatagram
		{
			public int ReceiverPort;
			public int SenderPort;

			public ConnectionRequestResponseDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				ReceiverPort = reader.ReadInt32();
				SenderPort = reader.ReadInt32();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(ReceiverPort);
				writer.Write(SenderPort);

				return stream.ToArray();
			}
		}


	}
}
