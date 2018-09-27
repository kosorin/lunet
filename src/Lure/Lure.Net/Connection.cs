using Lure.Collections;
using Lure.Extensions;
using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Messages;
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
    public class Connection : IDisposable
    {
        internal static byte SystemChannelId => 255;

        private readonly Peer _peer;
        private readonly IObjectPool<NetDataWriter> _messageWriterPool;
        private readonly byte _defaultChannelId;
        private readonly IDictionary<byte, INetChannel> _channels;

        private volatile ConnectionState _state;

        private long _lastReceivedMessageTimestamp;

        internal Connection(IPEndPoint remoteEndPoint, Peer peer)
        {
            _peer = peer;
            _messageWriterPool = new ObjectPool<NetDataWriter>(() => new NetDataWriter(_peer.Config.MessageBufferSize));

            _channels = _peer.Config.ChannelFactory.Create(this);
            if (_channels.ContainsKey(SystemChannelId))
            {
                throw new NetException($"Reserved channel {SystemChannelId}.");
            }
            _defaultChannelId = _channels.Keys.Min();

            _state = ConnectionState.NotConnected;

            RemoteEndPoint = remoteEndPoint;
        }


        public event TypedEventHandler<Connection> Disconnected;

        public event TypedEventHandler<Connection, NetMessage> MessageReceived;


        public ConnectionState State => _state;

        public IPEndPoint RemoteEndPoint { get; }

        // TODO: internal?
        public int MTU => 1000;

        // TODO: internal?
        public int RTT => 500;


        public void Connect()
        {
            if (_state == ConnectionState.NotConnected)
            {
                if (!_peer.IsRunning)
                {
                    throw new NetException("Peer is not ruuning.");
                }
                Log.Debug("Connecting {RemoteEndPoint}", RemoteEndPoint);
                _state = ConnectionState.Connecting;
                _peer.OnConnect(this);
            }
        }

        public void Disconnect()
        {
            if (_state == ConnectionState.Connected)
            {
                Log.Debug("Disconnecting {RemoteEndPoint}", RemoteEndPoint);
                _state = ConnectionState.Disconnecting;
                _peer.OnDisconnect(this);
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


        internal void Update()
        {
            if (_state == ConnectionState.Connected)
            {
                var now = Timestamp.Current;
                foreach (var (channelId, channel) in _channels)
                {
                    var receivedMessages = channel.GetReceivedMessages();
                    if (receivedMessages.Count > 0)
                    {
                        foreach (var data in receivedMessages)
                        {
                            var message = DeserializeMessage(data);
                            MessageReceived?.Invoke(this, message);
                        }
                        _lastReceivedMessageTimestamp = now;
                    }

                    foreach (var packet in channel.CollectOutgoingPackets())
                    {
                        _peer.SendPacket(RemoteEndPoint, channelId, packet);
                    }
                }

                if (now - _lastReceivedMessageTimestamp > _peer.Config.ConnectionTimeout)
                {
                    Disconnect();
                }
            }
        }

        internal void OnConnect()
        {
            if (_state == ConnectionState.NotConnected || _state == ConnectionState.Connecting)
            {
                _state = ConnectionState.Connected;
                _lastReceivedMessageTimestamp = Timestamp.Current;
                Log.Debug("Connected {RemoteEndPoint}", RemoteEndPoint);
            }
        }

        internal void OnDisconnect()
        {
            if (_state == ConnectionState.Connected || _state == ConnectionState.Disconnecting)
            {
                _state = ConnectionState.NotConnected;
                Log.Debug("Disconnected {RemoteEndPoint}", RemoteEndPoint);

                Disconnected?.Invoke(this);
            }
        }

        internal void OnReceivedPacket(byte channelId, NetDataReader reader)
        {
            if (_state == ConnectionState.Connected)
            {
                if (_channels.TryGetValue(channelId, out var channel))
                {
                    channel.ProcessIncomingPacket(reader);
                }
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
                    Disconnect();

                    _messageWriterPool.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
