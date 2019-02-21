using System;

namespace Lure.Net.Udp
{
    public class UdpServerConnection : UdpConnection
    {
        private readonly UdpSocket _socket;

        internal UdpServerConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory, UdpSocket socket) : base(remoteEndPoint, channelFactory)
        {
            _socket = socket;

            State = ConnectionState.Connected;
        }


        public override void Connect()
        {
            throw new InvalidOperationException($"{nameof(UdpServerConnection)} is automatically connected by listener.");
        }

        public override void Disconnect()
        {
            State = ConnectionState.Disconnected;
            OnDisconnected();
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
