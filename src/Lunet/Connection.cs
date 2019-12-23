using Lunet.Common;
using System;
using System.Threading;

namespace Lunet
{
    public abstract class Connection : IDisposable
    {
        private volatile ConnectionState _state;

        private readonly ChannelCollection _channels;

        protected Connection(InternetEndPoint remoteEndPoint, ChannelSettings channelSettings)
        {
            RemoteEndPoint = remoteEndPoint;

            State = ConnectionState.Disconnected;

            _channels = new ChannelCollection(channelSettings);
        }


        public int MTU => 100;

        public int RTT => 100;

        public ConnectionState State
        {
            get => _state;
            protected set => _state = value;
        }

        public InternetEndPoint RemoteEndPoint { get; }


        public event TypedEventHandler<Connection>? Disconnected;

        public event TypedEventHandler<IConnectionChannel, byte[]>? MessageReceived;


        public void Update()
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            ProcessChannels();
        }

        public abstract void Connect();

        public void SendMessage(byte channelId, byte[] data)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            var channel = _channels.Get(channelId, this);
            channel.SendMessage(data);
        }


        internal void HandleIncomingPacket(UdpPacket packet)
        {
            if (State != ConnectionState.Connected)
            {
                return;
            }

            var reader = packet.Reader;

            var packetType = (PacketType)reader.ReadByte();
            switch (packetType)
            {
                case PacketType.Channel:
                    var channelId = reader.ReadByte();
                    if (_channels.TryGet(channelId, this, out var channel))
                    {
                        channel.HandleIncomingPacket(reader);
                    }
                    break;
                case PacketType.System:
                    break;
                case PacketType.Fragment:
                    break;
                default:
                    break;
            }
        }

        internal abstract void HandleOutgoingPacket(UdpPacket packet);

        private protected abstract UdpPacket RentPacket();


        protected virtual void OnDisconnected()
        {
            Disconnected?.Invoke(this);
        }

        protected virtual void OnMessageReceived(IConnectionChannel channel, byte[] data)
        {
            MessageReceived?.Invoke(channel, data);
        }


        private void ProcessChannels()
        {
            foreach (var channel in _channels)
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
                    foreach (var outgoingPacket in outgoingPackets)
                    {
                        SendChannelPacket(channel, outgoingPacket);
                    }
                }
            }
        }

        private void SendChannelPacket(Channel channel, ChannelPacket channelPacket)
        {
            var packet = RentPacket();
            packet.RemoteEndPoint = RemoteEndPoint;

            packet.Writer.WriteByte((byte)PacketType.Channel);
            packet.Writer.WriteByte(channel.Id);
            channelPacket.SerializeHeader(packet.Writer);
            channelPacket.SerializeData(packet.Writer);

            HandleOutgoingPacket(packet);
        }


        private int _disposed;

        public virtual bool IsDisposed => _disposed == 1;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                // Nothing to dispose
            }
        }
    }
}
