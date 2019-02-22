using Lure.Net.Data;
using System;

namespace Lure.Net.Tcp
{
    public abstract class TcpConnection : Connection<InternetEndPoint>
    {
        private readonly TcpSocket _socket;

        internal TcpConnection(TcpSocket socket, IChannelFactory channelFactory) : base(socket.RemoteEndPoint, channelFactory)
        {
            _socket = socket;
            _socket.Disconnected += Socket_Disconnected;
            _socket.PacketReceived += Socket_PacketReceived;
        }


        public override void Connect()
        {
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
