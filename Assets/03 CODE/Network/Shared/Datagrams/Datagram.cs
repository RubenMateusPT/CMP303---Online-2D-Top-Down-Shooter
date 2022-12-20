using System;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

namespace OnlineShooter.Network.Shared.Datagrams
{
	public class Datagram
	{
		private Guid _id;
		private DateTime _timeStamp;

		private bool _isError;

		private byte _clientID;

		private DatagramType _type;

		private int _dataSize;
		private byte[] _data;
		private IDatagram _datagram;

		public Datagram(DatagramType type, IDatagram data, byte cliendID = 0, bool isError = false)
		{
			_id = Guid.NewGuid();
			_timeStamp = DateTime.Now;

			_isError = isError;

			_clientID = cliendID;

			_type = type;

			_data = data.ToArray();
			_dataSize = _data.Length;
			_datagram = data;
		}

		public Datagram(DatagramType type, IDatagram data, byte cliendID)
		:this(type, data, cliendID, false)
		{
		}

		public Datagram(DatagramType type, IDatagram data, bool isError)
			: this(type, data, 0, isError)
		{
		}

		public Datagram(byte[] bytes)
		{
			var reader = new BinaryReader(new MemoryStream(bytes));

			_id = Guid.Parse(reader.ReadString());
			_timeStamp = DateTime.Parse(reader.ReadString());
			_isError = reader.ReadBoolean();
			_clientID = reader.ReadByte();
			_type = (DatagramType)reader.ReadByte();
			_dataSize = reader.ReadInt32();
			_data = reader.ReadBytes(_dataSize);
		}

		public Guid GetPacketID => _id;
		public bool IsError => _isError;
		public byte GetClientID => _clientID;
		public DatagramType GetDatagramType() => _type;
		public byte[] GetData() => _data;
		public IDatagram GetDatagram() => _datagram;

		public byte[] ToArray()
		{
			var stream = new MemoryStream();
			var writer = new BinaryWriter(stream);

			writer.Write(_id.ToString());
			writer.Write(_timeStamp.ToString());
			writer.Write(_isError);
			writer.Write(_clientID);
			writer.Write((byte)_type);
			writer.Write(_dataSize);
			writer.Write(_data);

			return stream.ToArray();
		}
	}

}

