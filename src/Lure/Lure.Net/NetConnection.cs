using Lure.Collections;
using Lure.Extensions;
using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Messages;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public class NetConnection : IDisposable
    {
        internal static byte DefaultChannelId => 1;

        private readonly NetPeer _peer;

        private readonly IObjectPool<NetDataWriter> _messageWriterPool;
        private readonly IDictionary<byte, INetChannel> _channels;

        internal NetConnection(IPEndPoint remoteEndPoint, NetPeer peer)
        {
            _peer = peer;

            var dataWriterActivator = ObjectActivatorFactory.CreateWithValues<int, NetDataWriter>(_peer.Config.MessageBufferSize);
            _messageWriterPool = new ObjectPool<NetDataWriter>(dataWriterActivator);
            _channels = _peer.ChannelFactory.Create(this);

            RemoteEndPoint = remoteEndPoint;
        }

        public IPEndPoint RemoteEndPoint { get; }

        internal int MTU => 1000;


        internal void Update()
        {
            foreach (var (channelId, channel) in _channels)
            {
                foreach (var packet in channel.CollectOutgoingPackets())
                {
                    _peer.SendPacket(RemoteEndPoint, channelId, packet);
                }

                foreach (var data in channel.GetReceivedMessages())
                {
                    var message = DeserializeMessage(data);
                    if (message != null && message is DebugMessage testMessage && testMessage.Integer % 10 == 0)
                    {
                        Log.Information("Message: {Message}", message);
                    }
                }
            }
        }

        internal void OnReceivedPacket(byte channelId, INetDataReader reader)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.ProcessIncomingPacket(reader);
            }
        }


        public void SendMessage(NetMessage message)
        {
            SendMessage(DefaultChannelId, message);
        }

        public void SendMessage(byte channelId, NetMessage message)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
                var data = SerializeMessage(message);
                if (data.Length > MTU)
                {
                    throw new NetException("MTU");
                }
                channel.SendMessage(data);
            }
            else
            {
                throw new NetException("Unknown channel.");
            }
        }


        private NetMessage DeserializeMessage(byte[] data)
        {
            try
            {
                var reader = new NetDataReader(data);
                var typeId = reader.ReadUShort();
                var message = NetMessageManager.Create(typeId);
                message.DeserializeLib(reader);
                return message;
            }
            catch
            {
                // Just drop bad messages
                return null;
            }
        }

        private byte[] SerializeMessage(NetMessage message)
        {
            var writer = _messageWriterPool.Rent();
            try
            {
                writer.Reset();
                message.SerializeLib(writer);
                writer.Flush();

                return writer.GetBytes();
            }
            finally
            {
                _messageWriterPool.Return(writer);
            }
        }


        private bool _disposed;

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
                    _messageWriterPool.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
