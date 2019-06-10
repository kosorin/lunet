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

        public override void Disconnect()
        {
            State = ConnectionState.Disconnected;
            OnDisconnected();
        }


        internal override void HandleOutgoingPacket(ProtocolPacket packet)
        {
            _socket.SendPacket(RemoteEndPoint, packet.ChannelId, packet.ChannelPacket);
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
