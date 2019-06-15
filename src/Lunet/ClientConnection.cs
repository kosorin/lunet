using System;
using System.Threading;

namespace Lunet
{
    public class ClientConnection : Connection
    {
        private readonly UdpSocket _socket;

        public ClientConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory) : base(remoteEndPoint, channelFactory)
        {
            _socket = new UdpSocket(remoteEndPoint.IPVersion);
            _socket.PacketReceived += Socket_PacketReceived;

            State = ConnectionState.Disconnected;
        }


        public override void Connect()
        {
            State = ConnectionState.Connecting;
            _socket.Bind();

            State = ConnectionState.Connected;
        }


        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, IncomingProtocolPacket packet)
        {
            if (RemoteEndPoint == remoteEndPoint)
            {
                HandleIncomingPacket(packet);
            }
        }

        internal override void HandleOutgoingPacket(OutgoingProtocolPacket packet)
        {
            _socket.SendPacket(RemoteEndPoint, packet);
        }


        private int _disposed;

        public override bool IsDisposed => _disposed == 1;

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                State = ConnectionState.Disconnecting;
                _socket.Dispose();

                State = ConnectionState.Disconnected;
                OnDisconnected();
            }

            base.Dispose(disposing);
        }
    }
}
