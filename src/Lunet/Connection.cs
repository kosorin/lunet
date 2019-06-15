using Lunet.Common;
using Lunet.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lunet
{
    public abstract class Connection : IDisposable
    {
        private volatile ConnectionState _state;

        private readonly byte _defaultChannelId;
        private readonly IDictionary<byte, IChannel> _channels;

        private readonly ObjectPool<OutgoingProtocolPacket> _outgoingProtocolPacketPool;

        protected Connection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory)
        {
            RemoteEndPoint = remoteEndPoint;

            State = ConnectionState.Disconnected;

            _channels = channelFactory.Create(this).ToDictionary(x => x.Id);
            _defaultChannelId = _channels.Keys.Min();

            _outgoingProtocolPacketPool = new ObjectPool<OutgoingProtocolPacket>();
        }


        public int MTU => 100;

        public int RTT => 100;

        public ConnectionState State
        {
            get => _state;
            protected set => _state = value;
        }

        public InternetEndPoint RemoteEndPoint { get; }


        public event TypedEventHandler<Connection> Disconnected;

        public event TypedEventHandler<IChannel, byte[]> MessageReceived;


        public void Update()
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            foreach (var channel in _channels.Values)
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
                        var protocolPacket = _outgoingProtocolPacketPool.Rent();
                        protocolPacket.ChannelId = channel.Id;
                        protocolPacket.ChannelPacket = packet;

                        HandleOutgoingPacket(protocolPacket);
                    }
                }
            }
        }

        public abstract void Connect();

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


        internal void HandleIncomingPacket(IncomingProtocolPacket packet)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            if (_channels.TryGetValue(packet.ChannelId, out var channel))
            {
                channel.HandleIncomingPacket(packet.Reader);
            }
        }

        internal abstract void HandleOutgoingPacket(OutgoingProtocolPacket packet);


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
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _outgoingProtocolPacketPool.Dispose();
            }

            _disposed = true;
        }
    }
}
