using Lure.Collections;
using Lure.Extensions;
using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public class NetConnection : IDisposable
    {
        internal static byte SystemChannelId => 255;

        private readonly NetPeer _peer;
        private readonly IObjectPool<NetDataWriter> _messageWriterPool;
        private readonly byte _defaultChannelId;
        private readonly IDictionary<byte, INetChannel> _channels;

        private long _lastReceivedMessageTimestamp = Timestamp.Current;

        internal NetConnection(IPEndPoint remoteEndPoint, NetPeer peer)
        {
            _peer = peer;
            _messageWriterPool = new ObjectPool<NetDataWriter>(() => new NetDataWriter(_peer.Config.MessageBufferSize));

            _channels = _peer.Config.ChannelFactory.Create(this);
            if (_channels.ContainsKey(SystemChannelId))
            {
                throw new NetException($"Reserved channel {SystemChannelId}.");
            }
            _defaultChannelId = _channels.Keys.Min();

            RemoteEndPoint = remoteEndPoint;
            ReceivedMessages = new ConcurrentQueue<NetMessage>();
        }

        public IPEndPoint RemoteEndPoint { get; }


        internal ConcurrentQueue<NetMessage> ReceivedMessages { get; }

        internal int MTU => 1000;


        internal void Update()
        {
            var now = Timestamp.Current;
            foreach (var (channelId, channel) in _channels)
            {
                foreach (var packet in channel.CollectOutgoingPackets())
                {
                    _peer.SendPacket(RemoteEndPoint, channelId, packet);
                }

                var receivedMessages = channel.GetReceivedMessages();
                if (receivedMessages.Count > 0)
                {
                    foreach (var data in receivedMessages)
                    {
                        var message = DeserializeMessage(data);
                        ReceivedMessages.Enqueue(message);
                    }
                    _lastReceivedMessageTimestamp = now;
                }
            }

            if (now - _lastReceivedMessageTimestamp > _peer.Config.ConnectionTimeout * 1000)
            {
                _peer.RemoveConnection(this);
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
            SendMessage(_defaultChannelId, message);
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
