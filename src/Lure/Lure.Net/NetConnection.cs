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
        internal static byte SystemChannelId => 0;
        public static byte DefaultChannelId => 1;

        private const int KeepAliveTimeout = 1000;
        private const int DisconnectTimeout = 8000;
        private const int InitialMessageBufferSize = 32;

        private volatile NetConnectionState _state;

        private readonly ObjectPool<NetDataWriter> _messageWriterPool;
        private readonly ReliableOrderedChannel _systemChannel;
        private readonly Dictionary<byte, INetChannel> _channels;

        internal NetConnection(IPEndPoint remoteEndPoint, NetPeer peer)
        {
            _messageWriterPool = new ObjectPool<NetDataWriter>(() => new NetDataWriter(InitialMessageBufferSize));
            _systemChannel = new ReliableOrderedChannel(SystemChannelId, this);
            _channels = new Dictionary<byte, INetChannel>
            {
                [DefaultChannelId] = new ReliableOrderedChannel(DefaultChannelId, this),
            };

            _state = NetConnectionState.Disconnected;

            RemoteEndPoint = remoteEndPoint;
            Peer = peer;
        }

        public NetConnectionState State => _state;

        public IPEndPoint RemoteEndPoint { get; }

        public NetPeer Peer { get; }

        internal int MTU => 1000;


        internal void Update()
        {
            var lastOutgoingPacketTimestamp = 0L;
            var lastIncomingPacketTimestamp = 0L;

            foreach (var (channelId, channel) in _channels)
            {
                channel.Update();

                foreach (var data in channel.GetReceivedMessages())
                {
                    var message = DeserializeMessage(data);
                    if (message != null)
                    {
                        if (message is DebugMessage testMessage && testMessage.Integer % 10 == 0)
                        {
                            Log.Information("Message: {Message}", message);
                        }
                    }
                }
            }

            var now = Timestamp.Current;

            if (now - lastIncomingPacketTimestamp > DisconnectTimeout)
            {
            }
            if (now - lastOutgoingPacketTimestamp > KeepAliveTimeout)
            {
            }
        }


        internal void ProcessIncomingPacket(INetDataReader reader)
        {
            var channelId = reader.ReadByte();

            if (channelId == SystemChannelId)
            {
                _systemChannel.ProcessIncomingPacket(reader);
                return;
            }

            if (_state != NetConnectionState.Connected)
            {
                // Drop packets before connect
                return;
            }

            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.ProcessIncomingPacket(reader);
            }
        }


        internal void SendSystemMessage(SystemMessage message)
        {
            SendMessage(SystemChannelId, message);
        }

        public void SendMessage(NetMessage message)
        {
            SendMessage(DefaultChannelId, message);
        }

        public void SendMessage(byte channelId, NetMessage message)
        {
            if (channelId == SystemChannelId)
            {
                if (!(message is SystemMessage))
                {
                    throw new NetException("Using reserved system channel.");
                }
            }
            else
            {
                if (_state != NetConnectionState.Connected)
                {
                    // Drop packets before connect
                    return;
                }
            }

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

                    _systemChannel.Dispose();
                    foreach (var channel in _channels.Values)
                    {
                        channel.Dispose();
                    }
                }
                _disposed = true;
            }
        }
    }
}
