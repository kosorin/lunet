using Lure.Net.Common;
using Lure.Net.Common.Extensions;
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


        public event TypedEventHandler<IConnection> Disconnected;

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
                        HandleSendPacket(new ProtocolPacket
                        {
                            ChannelId = channelId,
                            ChannelPacket = packet,
                        });
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


        internal void HandleReceivedPacket(byte[] data, int offset, int length)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            var dataX = new ProtocolProcessor().Read(data, offset, length);
            if (dataX.Reader == null)
            {
                return;
            }

            if (_channels.TryGetValue(dataX.ChannelId, out var channel))
            {
                channel.HandleIncomingPacket(dataX.Reader);
            }
        }

        internal abstract void HandleSendPacket(ProtocolPacket packet);


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
