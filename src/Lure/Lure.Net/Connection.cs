using Lure.Extensions;
using Lure.Net.Channels;
using Lure.Net.Data;
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
        private readonly Peer _peer;
        private readonly byte _defaultChannelId;
        private readonly IDictionary<byte, INetChannel> _channels;

        private volatile ConnectionState _state;

        private long _lastReceivedMessageTimestamp;

        internal Connection(IPEndPoint remoteEndPoint, Peer peer, INetChannelFactory channelFactory)
        {
            _peer = peer;

            _channels = channelFactory.Create(this);
            _defaultChannelId = _channels.Keys.Min();

            _state = ConnectionState.NotConnected;

            RemoteEndPoint = remoteEndPoint;
        }


        public event TypedEventHandler<Connection> Disconnected;

        public event TypedEventHandler<Connection> Timeout;

        public event TypedEventHandler<INetChannel, byte[]> MessageReceived;


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

        public void SendMessage(byte[] data)
        {
            SendMessage(_defaultChannelId, data);
        }

        public void SendMessage(byte channelId, byte[] data)
        {
            if (_channels.TryGetValue(channelId, out var channel))
            {
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
                    if (receivedMessages?.Count > 0)
                    {
                        foreach (var data in receivedMessages)
                        {
                            MessageReceived?.Invoke(channel, data);
                        }
                        _lastReceivedMessageTimestamp = now;
                    }

                    var outgoingPackets = channel.CollectOutgoingPackets();
                    if (outgoingPackets?.Count > 0)
                    {
                        foreach (var packet in outgoingPackets)
                        {
                            _peer.SendPacket(RemoteEndPoint, channelId, packet);
                        }
                    }
                }

                if (now - _lastReceivedMessageTimestamp > _peer.Config.ConnectionTimeout)
                {
                    OnTimeout();
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

        private void OnTimeout()
        {
            Log.Debug("Timeout {RemoteEndPoint}", RemoteEndPoint);

            Timeout?.Invoke(this);
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
                }
                _disposed = true;
            }
        }
    }
}
