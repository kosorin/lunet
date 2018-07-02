using Lure.Collections;
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
    public sealed class NetConnection : IDisposable
    {
        private const int KeepAliveTimeout = 1000;
        private const int DisconnectTimeout = 8000;

        private readonly ObjectPool<NetDataWriter> _writerPool;

        private readonly NetPeer _peer;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly ReliableOrderedChannel _systemChannel;
        private readonly Dictionary<byte, INetChannel> _channels;

        internal NetConnection(NetPeer peer, IPEndPoint remoteEndPoint)
        {
            _writerPool = new ObjectPool<NetDataWriter>(() => new NetDataWriter(MTU));

            _peer = peer;
            _remoteEndPoint = remoteEndPoint;
            _systemChannel = new ReliableOrderedChannel(0, this);
            _channels = new Dictionary<byte, INetChannel>
            {
                [_systemChannel.Id] = _systemChannel,
            };
        }

        public NetPeer Peer => _peer;

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;

        internal int MTU => 1000;


        public void SendMessage(byte channeId, NetMessage message)
        {
            var channelId = reader.ReadByte();
            var channel = GetChannel(channelId);
            if (channel != null)
            {
                channel.ReceivePacket(reader);
                var data = SerializeMessage(message);
            }

            if (data.Length < MTU)
            {
                _systemChannel.SendMessage(data);
            }
        }

        public void Dispose()
        {
            _writerPool.Dispose();
            foreach (var channel in _channels.Values)
            {
                channel.Dispose();
            }
        }


        internal void Update()
        {
            var lastOutgoingPacketTimestamp = 0L;
            var lastIncomingPacketTimestamp = 0L;

            foreach (var channel in _channels.Values)
            {
                channel.Update();

                if (channel is IMessageChannel messageChannel)
                {
                    foreach (var rawMessage in messageChannel.GetReceivedRawMessages())
                    {
                        var message = DeserializeMessage(rawMessage.Data);
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

        internal void ReceivePacket(NetDataReader reader)
        {
            var channelId = reader.ReadByte();
            var channel = GetChannel(channelId);
            if (channel != null)
            {
                channel.ReceivePacket(reader);
            }
        }


        private INetChannel GetChannel(byte channeId)
        {
            if (_channels.TryGetValue(channeId, out var channel))
            {
                return channel;
            }
            else
            {
                return null;
            }
        }


        private NetMessage DeserializeMessage(byte[] data)
        {
            var reader = new NetDataReader(data);
            var typeId = reader.ReadUShort();
            var message = NetMessageManager.Create(typeId);
            message.Deserialize(reader);
            return message;
        }

        private byte[] SerializeMessage(NetMessage message)
        {
            var writer = _writerPool.Rent();
            try
            {
                writer.Reset();
                message.Serialize(writer);
                writer.Flush();

                return writer.GetBytes();
            }
            finally
            {
                _writerPool.Return(writer);
            }
        }
    }
}
