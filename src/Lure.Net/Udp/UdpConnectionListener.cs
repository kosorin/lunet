using Lure.Net.Data;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Udp
{
    public class UdpConnectionListener : ConnectionListener<InternetEndPoint, UdpServerConnection>
    {
        private readonly UdpSocket _socket;

        private readonly Dictionary<InternetEndPoint, UdpServerConnection> _connections = new Dictionary<InternetEndPoint, UdpServerConnection>();
        private readonly object _connectionsLock = new object();

        public UdpConnectionListener(ServerConfiguration config, IChannelFactory channelFactory) : base(channelFactory)
        {
            Config = config;

            _socket = new UdpSocket(Config);
            _socket.PacketReceived += Socket_PacketReceived;
        }


        protected ServerConfiguration Config { get; }


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


        private void Socket_PacketReceived(InternetEndPoint internetEndPoint, byte channelId, NetDataReader reader)
        {
            UdpServerConnection connection = null;
            lock (_connectionsLock)
            {
                _connections.TryGetValue(internetEndPoint, out connection);
                if (connection == null)
                {
                    if (_connections.Count >= Config.MaximumConnections)
                    {
                        return;
                    }

                    connection = new UdpServerConnection(internetEndPoint, ChannelFactory, _socket);
                    connection.Disconnected += Connection_Disconnected;
                    _connections.Add(internetEndPoint, connection);

                    OnNewConnection(connection);
                }
            }

            if (connection != null)
            {
                connection.HandleReceivedPacket(channelId, reader);
            }
        }

        private void Connection_Disconnected(IConnection<InternetEndPoint> connection)
        {
            lock (_connectionsLock)
            {
                _connections.Remove(connection.RemoteEndPoint);
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
