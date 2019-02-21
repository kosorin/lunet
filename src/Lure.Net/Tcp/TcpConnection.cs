using Lure.Net.Data;
using System;

namespace Lure.Net.Tcp
{
    public class TcpConnection : Connection<InternetEndPoint>
    {
        private readonly bool _isServer;
        private readonly TcpSocket _socket;

        public TcpConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory, ClientConfiguration config) : base(remoteEndPoint, channelFactory)
        {
            _socket = new TcpSocket(config, remoteEndPoint);
            _socket.Disconnected += Socket_Disconnected;
            _socket.PacketReceived += Socket_PacketReceived;

            _isServer = false;
        }

        internal TcpConnection(TcpSocket socket, IChannelFactory channelFactory) : base(socket.RemoteEndPoint, channelFactory)
        {
            _socket = socket;
            _socket.Disconnected += Socket_Disconnected;
            _socket.PacketReceived += Socket_PacketReceived;

            State = ConnectionState.Connected;
            _isServer = true;
        }


        public override void Connect()
        {
            if (_isServer)
            {
                throw new InvalidOperationException($"{nameof(TcpConnection)} is automatically connected by listener.");
            }

            State = ConnectionState.Connecting;
            _socket.Connect();

            State = ConnectionState.Connected;
        }

        public override void Disconnect()
        {
            State = ConnectionState.Disconnecting;
            _socket.Close();

            State = ConnectionState.Disconnected;
            OnDisconnected();
        }


        private void Socket_Disconnected(TcpSocket socket)
        {
            State = ConnectionState.Disconnected;
            OnDisconnected();
        }

        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, byte channelId, NetDataReader reader)
        {
            HandleReceivedPacket(channelId, reader);
        }

        internal override void SendPacket(byte channelId, IPacket packet)
        {
            _socket.SendPacket(channelId, packet);
        }


        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
