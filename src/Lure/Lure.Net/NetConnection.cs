using Lure.Collections;
using Lure.Extensions.NetCore;
using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public sealed class NetConnection : IDisposable
    {
        private const int ResendTimeout = 100;
        private const int KeepAliveTimeout = 1000;
        private const int DisconnectTimeout = 8000;

        private readonly ObjectPool<NetDataWriter> _writerPool;

        private readonly NetPeer _peer;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly UnreliableSequencedChannel _defaultChannel;
        private readonly Dictionary<byte, NetChannel> _channels;

        internal NetConnection(NetPeer peer, IPEndPoint remoteEndPoint)
        {
            _writerPool = new ObjectPool<NetDataWriter>(16, () => new NetDataWriter(MTU));

            _peer = peer;
            _remoteEndPoint = remoteEndPoint;
            _defaultChannel = new UnreliableSequencedChannel(0, this);
            _channels = new Dictionary<byte, NetChannel>
            {
                [_defaultChannel.Id] = _defaultChannel,
            };
        }

        public NetPeer Peer => _peer;

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;

        public int MTU => 1000;


        public void SendMessage(NetMessage message)
        {
            var rawMessage = SerializeMessage(message);

            if (rawMessage.Length < MTU)
            {
                _defaultChannel.SendRawMessage(rawMessage);
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

                if (lastOutgoingPacketTimestamp < channel.LastOutgoingPacketTimestamp)
                {
                    lastOutgoingPacketTimestamp = channel.LastOutgoingPacketTimestamp;
                }
                if (lastIncomingPacketTimestamp < channel.LastIncomingPacketTimestamp)
                {
                    lastIncomingPacketTimestamp = channel.LastIncomingPacketTimestamp;
                }
            }

            var now = Timestamp.Current;

            if (Peer.IsServer)
            {
                if (now - lastIncomingPacketTimestamp > DisconnectTimeout)
                {
                    Peer.Disconnect(this);
                }
            }
            else
            {
                if (now - lastOutgoingPacketTimestamp > KeepAliveTimeout)
                {
                }
            }
        }

        private NetChannel GetChannel(byte channeId)
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

        internal void ReceivePacket(NetDataReader reader)
        {
            var channelId = reader.ReadByte();
            var channel = GetChannel(channelId);
            if (channel != null)
            {
                channel.ReceivePacket(reader);
            }
        }
    }
}
