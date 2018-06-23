using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal abstract class PayloadChannel<TPacket, T> : NetChannel
        where TPacket : Packet
        where TPacketData : PacketData
    {
        protected readonly PacketDataType _dataType;
        protected readonly ObjectPool<TPacketData> _dataPool;
        protected readonly ObjectPool<TPacket> _packetPool;

        private bool _disposed;

        protected PayloadChannel(byte id, NetConnection connection, PacketDataType dataType) : base(id, connection)
        {
            _dataType = dataType;
            _dataPool = new ObjectPool<TPacketData>();
            _packetPool = new ObjectPool<TPacket>();
            _packetPool.Returned += PacketPool_Returned;
        }

        public abstract void SendRawMessage(byte[] rawMessage);

        public sealed override void ReceivePacket(NetDataReader reader)
        {
            var packet = _packetPool.Rent();

            packet.DeserializeHeader(reader);

            if (packet.DataType != _dataType)
            {
                return;
            }
            if (!AcceptIncomingPacket(packet))
            {
                return;
            }

            packet.Data = _dataPool.Rent();
            packet.DeserializeData(reader);

            Log.Verbose("[{RemoteEndPoint}] Data <<< {DataType}", _connection.RemoteEndPoint, packet.DataType);

            ParseRawMessages(packet);

            _packetPool.Return(packet);
            LastIncomingPacketTimestamp = Timestamp.Current;
        }

        public override void Update()
        {
            foreach (var packet in CollectOutgoingData().Select(CreateOutgoingPacket))
            {
                SendPacket(packet);
            }
        }

        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract void PrepareOutgoingPacket(TPacket packet);

        protected abstract List<TPacketData> CollectOutgoingData();

        protected abstract void ParseRawMessages(TPacket packet);

        protected TPacket CreateOutgoingPacket(TPacketData data)
        {
            var packet = _packetPool.Rent();
            packet.ChannelId = _id;
            packet.DataType = _dataType;
            packet.Data = data;

            PrepareOutgoingPacket(packet);

            return packet;
        }

        protected void SendPacket(TPacket packet)
        {
            _connection.Peer.SendPacket(_connection, packet);

            Log.Verbose("[{RemoteEndPoint}] Data >>> {DataType}", _connection.RemoteEndPoint, packet.DataType);

            _packetPool.Return(packet);
            LastOutgoingPacketTimestamp = Timestamp.Current;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _packetPool.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        private void PacketPool_Returned(object sender, TPacket packet)
        {
            var data = (TPacketData)packet.Data;
            if (data != null)
            {
                packet.Data = null;
                _dataPool.Return(data);
            }
        }
    }
}
