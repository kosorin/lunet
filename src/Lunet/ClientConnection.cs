using System;
using System.Threading;

namespace Lunet
{
    public class ClientConnection : Connection
    {
        private readonly UdpSocket _socket;

        public ClientConnection(InternetEndPoint remoteEndPoint) : this(remoteEndPoint, ChannelSettings.Default)
        {
        }

        public ClientConnection(InternetEndPoint remoteEndPoint, ChannelSettings channelSettings) : base(remoteEndPoint, channelSettings)
        {
            _socket = new UdpSocket(remoteEndPoint.EndPoint.AddressFamily);
            _socket.PacketReceived += Socket_PacketReceived;

            State = ConnectionState.Disconnected;
        }


        public override void Connect()
        {
            State = ConnectionState.Connecting;
            _socket.Bind();

            State = ConnectionState.Connected;
        }


        private void Socket_PacketReceived(UdpSocket socket, UdpPacket packet)
        {
            if (RemoteEndPoint == packet.RemoteEndPoint)
            {
                HandleIncomingPacket(packet);
            }
        }

        internal override void HandleOutgoingPacket(UdpPacket packet)
        {
            packet.RemoteEndPoint = RemoteEndPoint;
            _socket.SendPacket(packet);
        }

        private protected override UdpPacket RentPacket()
        {
            return _socket.RentPacket();
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
