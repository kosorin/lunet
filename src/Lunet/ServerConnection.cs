using System;
using System.Threading;

namespace Lunet
{
    internal class ServerConnection : Connection
    {
        private readonly UdpSocket _socket;

        internal ServerConnection(UdpSocket socket, InternetEndPoint remoteEndPoint, IChannelFactory channelFactory) : base(remoteEndPoint, channelFactory)
        {
            _socket = socket;

            State = ConnectionState.Connected;
        }


        public override void Connect()
        {
            throw new InvalidOperationException($"{nameof(ServerConnection)} is automatically connected by listener.");
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
                State = ConnectionState.Disconnected;
                OnDisconnected();
            }

            base.Dispose(disposing);
        }
    }
}
