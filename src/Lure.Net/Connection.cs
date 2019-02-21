using Lure.Extensions;
using Lure.Net.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net
{
    public abstract class Connection<TEndPoint> : IConnection<TEndPoint>
        where TEndPoint : IEndPoint
    {
        private volatile ConnectionState _state;

        private readonly byte _defaultChannelId;
        private readonly IDictionary<byte, IChannel> _channels;

        protected Connection(TEndPoint remoteEndPoint, IChannelFactory channelFactory)
        {
            RemoteEndPoint = remoteEndPoint;

            State = ConnectionState.Disconnected;

            _channels = channelFactory.Create(this).ToDictionary(x => x.Id);
            _defaultChannelId = _channels.Keys.Min();
        }


        public int MTU => 100;

        public int RTT => 100;

        public ConnectionState State
        {
            get => _state;
            protected set => _state = value;
        }

        public TEndPoint RemoteEndPoint { get; }

        IEndPoint IConnection.RemoteEndPoint => RemoteEndPoint;


        public event TypedEventHandler<IConnection<TEndPoint>> Disconnected;

        event TypedEventHandler<IConnection> IConnection.Disconnected
        {
            add => Disconnected += value;
            remove => Disconnected -= value;
        }

        public event TypedEventHandler<IChannel, byte[]> MessageReceived;


        public void Update()
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            foreach (var (channelId, channel) in _channels)
            {
                var receivedMessages = channel.GetReceivedMessages();
                if (receivedMessages?.Count > 0)
                {
                    foreach (var data in receivedMessages)
                    {
                        OnMessageReceived(channel, data);
                    }
                }

                var outgoingPackets = channel.CollectOutgoingPackets();
                if (outgoingPackets?.Count > 0)
                {
                    foreach (var packet in outgoingPackets)
                    {
                        SendPacket(channelId, packet);
                    }
                }
            }
        }

        public abstract void Connect();

        public abstract void Disconnect();

        public void SendMessage(byte[] data)
        {
            SendMessage(_defaultChannelId, data);
        }

        public void SendMessage(byte channelId, byte[] data)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.SendMessage(data);
            }
            else
            {
                throw new NetException("Unknown channel.");
            }
        }


        internal void HandleReceivedPacket(byte channelId, NetDataReader reader)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            if (_channels.TryGetValue(channelId, out var channel))
            {
                channel.HandleIncomingPacket(reader);
            }
        }

        internal abstract void SendPacket(byte channelId, IPacket packet);


        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this);
        }

        protected virtual void OnMessageReceived(IChannel channel, byte[] data)
        {
            MessageReceived?.Invoke(channel, data);
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
