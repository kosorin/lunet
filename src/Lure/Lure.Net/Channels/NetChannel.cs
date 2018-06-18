using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Packets;
using System;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel : IDisposable
    {
        protected readonly byte _id;
        protected readonly NetConnection _connection;

        protected NetChannel(byte id, NetConnection connection)
        {
            _id = id;
            _connection = connection;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        internal abstract void ReceivePacket(NetDataReader reader);

        protected virtual void Dispose(bool disposing)
        {
        }
    }

    internal abstract class NetChannel<TPacket> : NetChannel
        where TPacket : Packet, new()
    {
        protected ObjectPool<TPacket> _packetPool;
        protected PacketDataPool _packetDataPool;

        private bool _disposed;

        protected NetChannel(byte id, NetConnection connection) : base(id, connection)
        {
            _packetPool = new ObjectPool<TPacket>(() => new TPacket());
            _packetPool.Returned += PacketPool_Returned;
            _packetDataPool = new PacketDataPool();
        }

        internal sealed override void ReceivePacket(NetDataReader reader)
        {
            var type = (PacketDataType)reader.ReadByte();
            var packet = CreateIncomingPacket(type);
            if (packet != null)
            {
                reader.ReadSerializable(packet);

                //Logger.Verbose("[{RemoteEndPoint}] Received data (size={Size}): {Type} {Seq}", token.RemoteEndPoint, token.BytesTransferred, packet.Type, packet.Seq);

                OnPacketReceived(packet);

                _packetPool.Return(packet);
            }
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

        protected abstract void OnPacketReceived(TPacket packet);

        protected TPacket CreateIncomingPacket(PacketDataType type)
        {
            var packet = _packetPool.Rent();
            packet.ChannelId = _id;
            packet.Type = type;
            packet.Data = _packetDataPool.Rent(type);
            return packet;
        }

        protected TPacket CreateOutgoingPacket<TPacketData>() where TPacketData : PacketData
        {
            var packet = _packetPool.Rent();
            packet.ChannelId = _id;
            packet.Data = _packetDataPool.Rent<TPacketData>();
            PrepareOutgoingPacket(packet);
            return packet;
        }

        protected abstract void PrepareOutgoingPacket(TPacket packet);

        private void PacketPool_Returned(object sender, TPacket packet)
        {
            if (packet.Data != null)
            {
                _packetDataPool.Return(packet.Data);
                packet.Data = null;
            }
        }
    }
}
