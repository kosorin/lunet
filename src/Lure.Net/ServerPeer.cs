using Lure.Net.Data;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class ServerPeer : Peer
    {
        private readonly ServerConfiguration _config;
        private readonly ConcurrentDictionary<IPEndPoint, Connection> _connections;

        public ServerPeer(int localPort, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new ServerConfiguration
            {
                LocalPort = localPort,
                AddressFamily = addressFamily,
            }, null)
        {
        }

        public ServerPeer(ServerConfiguration config, IChannelFactory channelFactory)
            : base(config, channelFactory)
        {
            _config = config;

            _connections = new ConcurrentDictionary<IPEndPoint, Connection>();
        }


        public event TypedEventHandler<ServerPeer, Connection> NewConnection;


        public new ServerConfiguration Config => _config;


        protected override void OnStop()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Disconnect();
                connection.Dispose();
            }
            _connections.Clear();
            base.OnStop();
        }

        protected override void OnUpdate()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Update();
            }
        }


        internal override void OnConnect(Connection connection)
        {
            connection.OnConnect();
            NewConnection?.Invoke(this, connection);
        }

        internal override void OnDisconnect(Connection connection)
        {
            if (_connections.TryRemove(connection.RemoteEndPoint, out connection))
            {
                connection.OnDisconnect();
                connection.Dispose();
            }
        }

        internal override void OnPacketReceived(IPEndPoint remoteEndPoint, byte channelId, NetDataReader reader)
        {
            _connections.TryGetValue(remoteEndPoint, out var connection);
            if (connection == null)
            {
                if (_connections.Count >= Config.MaximumConnections)
                {
                    return;
                }

                connection = new Connection(remoteEndPoint, this, ChannelFactory);
                if (_connections.TryAdd(remoteEndPoint, connection))
                {
                    OnConnect(connection);
                }
                else
                {
                    connection.Dispose();
                    connection = null;
                }
            }

            if (connection != null)
            {
                connection.OnReceivedPacket(channelId, reader);
            }
        }
    }
}
