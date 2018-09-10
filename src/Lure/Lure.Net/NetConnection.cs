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

        private const int InitialMessageBufferSize = 32;

        private readonly ObjectPool<NetDataWriter> _messageWriterPool;
        private readonly INetChannel _systemChannel;
        private readonly Dictionary<byte, INetChannel> _channels;
        private volatile NetConnectionState _state;

        private string _challengeCode;

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


        internal void Connect()
        {
            if (_state == NetConnectionState.Disconnected)
            {
                var message = NetMessageManager.Create<ConnectionRequestMessage>();
                SendSystemMessage(message);
                _state = NetConnectionState.Connecting;
            }
        }

        internal void Disconnect()
        {
            if (_state == NetConnectionState.Connected)
            {
                var message = NetMessageManager.Create<DisconnectMessage>();
                SendSystemMessage(message);
            }
            _state = NetConnectionState.Disconnected;
        }

        internal void Update()
        {
            SystemUpdate();

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
        }

        private void SystemUpdate()
        {
            _systemChannel.Update();

            foreach (var data in _systemChannel.GetReceivedMessages())
            {
                var systemMessage = DeserializeMessage(data) as SystemMessage;
                if (systemMessage != null)
                {
                    Log.Information("...");
                    switch (systemMessage.Type)
                    {
                    case SystemMessageType.ConnectionRequest:
                        if (_state == NetConnectionState.Disconnected)
                        {
                            _challengeCode = "TEST";
                            var challengeMessage = NetMessageManager.Create<ConnectionChallengeMessage>();
                            SendSystemMessage(challengeMessage);
                            _state = NetConnectionState.Connecting;
                            Log.Information("ConnectionRequest");
                        }
                        break;
                    case SystemMessageType.ConnectionChallenge:
                        if (_state == NetConnectionState.Connecting && _challengeCode == null)
                        {
                            _challengeCode = "TEST";
                            var responseMessage = NetMessageManager.Create<ConnectionResponseMessage>();
                            SendSystemMessage(responseMessage);
                            Log.Information("ConnectionChallenge");
                        }
                        break;
                    case SystemMessageType.ConnectionResponse:
                        if (_state == NetConnectionState.Connecting && _challengeCode != null)
                        {
                            _challengeCode = "TEST";
                            var acceptMessage = NetMessageManager.Create<ConnectionAcceptMessage>();
                            SendSystemMessage(acceptMessage);
                            _state = NetConnectionState.Connected;
                            Log.Information("ConnectionResponse");
                        }
                        break;
                    case SystemMessageType.ConnectionAccept:
                        _state = NetConnectionState.Connected;
                        Log.Information("ConnectionAccept");
                        break;
                    case SystemMessageType.ConnectionReject:
                        _state = NetConnectionState.Error;
                        Log.Information("ConnectionReject");
                        break;

                    case SystemMessageType.Disconnect:
                        _state = NetConnectionState.Disconnected;
                        Log.Information("Disconnected");
                        break;

                    default:
                        break;
                    }
                }
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
            _systemChannel.Update();
        }

        public void SendMessage(NetMessage message)
        {
            SendMessage(DefaultChannelId, message);
        }

        public void SendMessage(byte channelId, NetMessage message)
        {
            INetChannel channel;
            if (channelId == SystemChannelId)
            {
                if (message is SystemMessage)
                {
                    channel = _systemChannel;
                }
                else
                {
                    throw new NetException("Using reserved system channel.");
                }
            }
            else
            {
                if (_state == NetConnectionState.Connected)
                {
                    _channels.TryGetValue(channelId, out channel);
                }
                else
                {
                    // Drop messages before connect
                    return;
                }
            }

            if (channel != null)
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
