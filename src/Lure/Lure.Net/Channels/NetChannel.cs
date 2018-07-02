using Lure.Collections;
using Lure.Net.Data;
using System;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel : INetChannel
    {
        protected readonly byte _id;
        protected readonly NetConnection _connection;

        private bool _disposed;

        protected NetChannel(byte id, NetConnection connection)
        {
            _id = id;
            _connection = connection;
        }

        public byte Id => _id;


        public abstract void Update();

        public abstract void ReceivePacket(NetDataReader reader);

        public void Dispose()
        {
            Dispose(true);
        }


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
