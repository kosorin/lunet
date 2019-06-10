using Lunet.Data;
using System;

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

        public override void Disconnect()
        {
            State = ConnectionState.Disconnecting;
            _socket.Close();

            State = ConnectionState.Disconnected;
            OnDisconnected();
        }


        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, NetDataReader reader)
        {
            if (RemoteEndPoint == remoteEndPoint)
            {
                HandleIncomingPacket(reader);
            }
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
