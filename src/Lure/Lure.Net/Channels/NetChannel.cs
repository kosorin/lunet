using Lure.Collections;
using Lure.Net.Data;
using System;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel : IDisposable, INetChannel
    {
        protected readonly byte _id;
        protected readonly NetConnection _connection;

        private bool _disposed;

        protected NetChannel(byte id, NetConnection connection)
        {
            _id = id;
            _connection = connection;

            var now = Timestamp.Current;
            LastOutgoingPacketTimestamp = now;
            LastIncomingPacketTimestamp = now;
        }

        public byte Id => _id;

        public long LastOutgoingPacketTimestamp { get; protected set; }

        public long LastIncomingPacketTimestamp { get; protected set; }

        public void Dispose()
        {
            Dispose(true);
        }

        public abstract void Update();

        public abstract void ReceivePacket(NetDataReader reader);


        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
                _disposed = true;
            }
        }
    }
}
