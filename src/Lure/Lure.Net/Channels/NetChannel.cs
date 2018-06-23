﻿using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel : IDisposable
    {
        protected readonly byte _id;
        protected readonly NetConnection _connection;

        private readonly ObjectPool<NetDataWriter> _writerPool;

        private bool _disposed;

        protected NetChannel(byte id, NetConnection connection)
        {
            _id = id;
            _connection = connection;

            _writerPool = new ObjectPool<NetDataWriter>();

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
                    _writerPool.Dispose();
                }
                _disposed = true;
            }
        }
    }
}