using System.Collections.Generic;

namespace Lunet.Udp
{
    public class UdpConnectionListener : ConnectionListener
    {
        private readonly UdpSocket _socket;

        private readonly Dictionary<InternetEndPoint, UdpServerConnection> _connections = new Dictionary<InternetEndPoint, UdpServerConnection>();
        private readonly object _connectionsLock = new object();

        public UdpConnectionListener(InternetEndPoint localEndPoint, IChannelFactory channelFactory) : base(channelFactory)
        {
            _socket = new UdpSocket(localEndPoint);
            _socket.PacketReceived += Socket_PacketReceived;
        }


        public override void Start()
        {
            _socket.Bind();
        }

        public override void Stop()
        {
            lock (_connectionsLock)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.Disconnect();
                }
            }
            _socket.Close();
        }


        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, byte[] data, int offset, int length)
        {
            UdpServerConnection connection = null;
            lock (_connectionsLock)
            {
                _connections.TryGetValue(remoteEndPoint, out connection);
                if (connection == null)
                {
                    connection = new UdpServerConnection(_socket, remoteEndPoint, ChannelFactory);
                    connection.Disconnected += Connection_Disconnected;
                    _connections.Add(remoteEndPoint, connection);

                    OnNewConnection(connection);
                }
            }

            connection.HandleReceivedPacket(data, offset, length);
        }

        private void Connection_Disconnected(IConnection connection)
        {
            lock (_connectionsLock)
            {
                _connections.Remove((InternetEndPoint)connection.RemoteEndPoint);
                connection.Disconnected -= Connection_Disconnected;
            }
        }


        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Stop();
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
