using System;

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


        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                State = ConnectionState.Disconnected;
                OnDisconnected();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
