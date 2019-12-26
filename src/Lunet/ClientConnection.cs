using System;
using System.Threading;

namespace Lunet
{
    public class ClientConnection : Connection
    {
        private readonly UdpSocket _socket;

        public ClientConnection(UdpEndPoint remoteEndPoint) : this(remoteEndPoint, ChannelSettings.Default)
        {
        }

        public ClientConnection(UdpEndPoint remoteEndPoint, ChannelSettings channelSettings) : base(remoteEndPoint, channelSettings)
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


        private protected override void SendPacket(UdpPacket packet)
        {
            _socket.SendPacket(packet);
        }

        private protected override UdpPacket RentPacket()
        {
            var packet = _socket.RentPacket();
            packet.RemoteEndPoint = RemoteEndPoint;
            return packet;
        }


        private void Socket_PacketReceived(UdpSocket socket, UdpPacket packet)
        {
            if (RemoteEndPoint == packet.RemoteEndPoint)
            {
                HandleIncomingPacket(packet);
            }
        }


        private int _disposed;

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
