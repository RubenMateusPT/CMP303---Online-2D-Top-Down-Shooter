using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json.Linq;
using OnlineShooter.Network.Shared.Errors;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using static NetworkClient;

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
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 3,
					TimeToResend = 5
				};
			}

			public Guid RequestPacketGUID;
			public NetworkError Error;
			public string ErrorMessage;

			public ErrorDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());
				Error = (NetworkError) reader.ReadByte();
				ErrorMessage = reader.ReadString();

				
			}


			public void OnFailedSent()
			{
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());
				writer.Write((byte)Error);
				writer.Write(ErrorMessage);

				return stream.ToArray();
			}
		}

		public struct EmptyDatagram : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 0,
					TimeToResend = 0
				};
			}

			public Action OnFailAction;

			public void OnFailedSent()
			{
				if (OnFailAction == null) return;
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				return Array.Empty<byte>();
			}
		}

		public struct AcknowledgeDatagram : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 7,
					TimeToResend = 2
				};
			}

			public Guid RequestPacketGUID;

			public AcknowledgeDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());
				
			}

			public void OnFailedSent()
			{
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());

				return stream.ToArray();
			}
		}

		public struct ConnectionRequestDatagram : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 5
				};
			}

			public Action OnFailAction;

			public string PlayerName;
			public string PlayerColor;

			public IPEndPoint Receiver;

			public ConnectionRequestDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				PlayerName = reader.ReadString();
				PlayerColor = reader.ReadString();
				Receiver = ParseEndPoint(reader.ReadString());

				OnFailAction = null;
				
			}

			public void OnFailedSent()
			{
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(PlayerName);
				writer.Write(PlayerColor);
				writer.Write(Receiver.ToString());

				return stream.ToArray();
			}
		}

		public struct ConnectionRequestResponseDatagram : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 5
				};
			}

			public Action OnFailAction;
			public Guid RequestPacketGUID;
			public int ReceiverPort;
			public int SenderPort;

			public ConnectionRequestResponseDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());
				ReceiverPort = reader.ReadInt32();
				SenderPort = reader.ReadInt32();

				OnFailAction = null;
				
			}

			public void OnFailedSent()
			{
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());
				writer.Write(ReceiverPort);
				writer.Write(SenderPort);

				return stream.ToArray();
			}
		}

		public struct RequestGameDataDatagram : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 5
				};
			}

			public Action OnFailAction;
			public Guid RequestPacketGUID;
			public RequestGameDataDatagram(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());
				OnFailAction = null;

				
			}

			public void OnFailedSent()
			{
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());


				return stream.ToArray();
			}
		}

		[Serializable]
		public struct GameDataDatagram : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 5
				};
			}

			[NonSerialized]public Action OnFailAction;
			public Guid RequestPacketGUID;

			public List<SerializableNetworkClient> Players;

			public GameDataDatagram(byte[] bytes)
			{
				var stream = new MemoryStream(bytes);
				var formatter = new BinaryFormatter();
				var temp = (GameDataDatagram)formatter.Deserialize(stream);

				RequestPacketGUID = temp.RequestPacketGUID;
				Players = temp.Players;

				OnFailAction = null;
				
			}

			public void OnFailedSent()
			{
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				var formatter = new BinaryFormatter();
				var stream = new MemoryStream();
				formatter.Serialize(stream,this);
				return stream.ToArray();
			}
		}

		public struct NewPlayerJoin : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 7
				};
			}

			public Action OnFailAction;
			public Guid RequestPacketGUID;
			public NewPlayerJoin(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());
				OnFailAction = null;

				
			}

			public void OnFailedSent()
			{
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());


				return stream.ToArray();
			}
		}

		public struct NewPlayerJoinResponse : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 5
				};
			}

			public Guid RequestPacketGUID;
			public NewPlayerJoinResponse(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());

				
			}

			public void OnFailedSent()
			{
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());

				return stream.ToArray();
			}
		}

		public struct NewPlayerGroupRequest : IDatagram
		{
			
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 5
				};
			}

			public Action OnFailAction;
			public SerializableNetworkClient Client;

			public NewPlayerGroupRequest(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));
				OnFailAction = null;

				Client = new SerializableNetworkClient()
				{
					Id = reader.ReadByte(),
					Name = reader.ReadString(),
					Color = reader.ReadString()
				};

				
			}

			public void OnFailedSent()
			{
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(Client.Id);
				writer.Write(Client.Name);
				writer.Write(Client.Color);

				return stream.ToArray();
			}
		}

		public struct NewPlayerGroupResponse : IDatagram
		{
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 3,
					TimeToResend = 5
				};
			}

			public Guid RequestPacketGUID;
			public NewPlayerGroupResponse(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				RequestPacketGUID = Guid.Parse(reader.ReadString());

				
			}

			public void OnFailedSent()
			{
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(RequestPacketGUID.ToString());

				return stream.ToArray();
			}
		}

		public struct RemoveClientDatagram : IDatagram
		{
			public Action OnFailAction;
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 5,
					TimeToResend = 3
				};
			}

			public void OnFailedSent()
			{
				if (OnFailAction == null) return;
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				return new byte[0];
			}
		}

		public struct AreYouAliveDatagram : IDatagram
		{
			public Action OnFailAction;
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 3,
					TimeToResend = 2
				};
			}

			public void OnFailedSent()
			{
				if (OnFailAction == null) return;
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				return new byte[0];
			}
		}

		public struct DisconnectRequest : IDatagram
		{
			public Action OnFailAction;
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 3,
					TimeToResend = 2
				};
			}

			public void OnFailedSent()
			{
				if (OnFailAction == null) return;
				OnFailAction.Invoke();
			}

			public byte[] ToArray()
			{
				return new byte[0];
			}
		}


		public struct PlayerMovement : IDatagram
		{
			public NetworkPacketManager.PacketSettings GetPacketSettings()
			{
				return new NetworkPacketManager.PacketSettings
				{
					MaxRetries = 0,
					TimeToResend = 0
				};
			}

			public Vector2 Pos;
			public float Angle;

			public PlayerMovement(byte[] bytes)
			{
				var reader = new BinaryReader(new MemoryStream(bytes));

				Pos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
				Angle = reader.ReadSingle();
			}

			public void OnFailedSent()
			{
			}

			public byte[] ToArray()
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);

				writer.Write(Pos.x);
				writer.Write(Pos.y);
				writer.Write(Angle);

				return stream.ToArray();
			}
		}
	}
}
