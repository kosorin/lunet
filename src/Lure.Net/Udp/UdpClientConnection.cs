using System;

namespace Lure.Net.Udp
{
    public class UdpClientConnection : UdpConnection
    {
        private readonly UdpSocket _socket;

        public UdpClientConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory, ClientConfiguration config) : base(remoteEndPoint, channelFactory)
        {
            _socket = new UdpSocket(config);
            _socket.PacketReceived += Socket_PacketReceived;
        }


        public override void Connect()
        {
            State = ConnectionState.Connecting;
            _socket.Bind();

            State = ConnectionState.Connected;
        }

        public override void Disconnect()
        {
            State = ConnectionState.Disconnecting;
            _socket.Close();

            State = ConnectionState.Disconnected;
            OnDisconnected();
        }


        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, byte channelId, Data.NetDataReader reader)
        {
            if (RemoteEndPoint.Equals(remoteEndPoint))
            {
                HandleReceivedPacket(channelId, reader);
            }
        }

        internal override void SendPacket(byte channelId, IPacket packet)
        {
            _socket.SendPacket(RemoteEndPoint, channelId, packet);
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
