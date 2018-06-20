using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Packets;
using Serilog;
using System;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel : IDisposable
    {
        protected readonly byte _id;
        protected readonly NetConnection _connection;
        protected long _lastPacketTimestamp;

        protected NetChannel(byte id, NetConnection connection)
        {
            _id = id;
            _connection = connection;
        }

        public byte Id => _id;

        public void Dispose()
        {
            Dispose(true);
        }

        public abstract void Update();

        public abstract void ReceivePacket(NetDataReader reader);

        protected virtual void Dispose(bool disposing)
        {
        }
    }

    internal abstract class NetChannel<TPacket> : NetChannel
        where TPacket : Packet
    {
        protected ObjectPool<TPacket> _packetPool;
        protected PacketDataPool _packetDataPool;

        private bool _disposed;

        protected NetChannel(byte id, NetConnection connection) : base(id, connection)
        {
            _packetPool = new ObjectPool<TPacket>();
            _packetDataPool = new PacketDataPool();
        }

        public sealed override void ReceivePacket(NetDataReader reader)
        {
            var packet = _packetPool.Rent();

            packet.DeserializeHeader(reader);

            if (!AcceptPacket(packet))
            {
                return;
            }

            packet.Data = _packetDataPool.Rent(packet.DataType);
            packet.DeserializeData(reader);

            Log.Verbose("[{RemoteEndPoint}] Data <<< ({PacketData})", _connection.RemoteEndPoint, packet.Data.DebuggerDisplay);

            _packetPool.Return(packet);
        }

        protected abstract bool AcceptPacket(TPacket packet);

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

        protected TPacket CreateOutgoingPacket()
        {
            var packet = _packetPool.Rent();
            packet.ChannelId = _id;
            PrepareOutgoingPacket(packet);
            return packet;
        }

        protected abstract void PrepareOutgoingPacket(TPacket packet);
    }
}
