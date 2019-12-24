using System;
using System.Threading;

namespace Lunet
{
    internal class ServerConnection : Connection
    {
        private readonly UdpSocket _socket;

        internal ServerConnection(UdpSocket socket, InternetEndPoint remoteEndPoint, ChannelSettings channelSettings) : base(remoteEndPoint, channelSettings)
        {
            _socket = socket;

            State = ConnectionState.Connected;
        }


        public override void Connect()
        {
            throw new InvalidOperationException($"{nameof(ServerConnection)} is automatically connected by listener.");
        }


        internal override void HandleOutgoingPacket(UdpPacket packet)
        {
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
                State = ConnectionState.Disconnected;
                OnDisconnected();
            }

            base.Dispose(disposing);
        }
    }
}
