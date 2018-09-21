using Lure.Net.Data;
using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class ServerPeer : Peer
    {
        private readonly ServerPeerConfig _config;
        private readonly ConcurrentDictionary<IPEndPoint, Connection> _connections;

        public ServerPeer(int localPort, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new ServerPeerConfig
            {
                LocalPort = localPort,
                AddressFamily = addressFamily,
            })
        {
        }

        public ServerPeer(ServerPeerConfig config)
            : base(config)
        {
            _config = config;

            _connections = new ConcurrentDictionary<IPEndPoint, Connection>();
        }


        public event TypedEventHandler<ServerPeer, Connection> NewConnection;


        public new ServerPeerConfig Config => _config;


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


        internal override void OnDisconnect(Connection connection)
        {
            if (_connections.TryRemove(connection.RemoteEndPoint, out connection))
            {
                connection.OnDisconnect();
                connection.Dispose();
            }
        }

        internal override void OnPacketReceived(IPEndPoint remoteEndPoint, byte channelId, INetDataReader reader)
        {
            _connections.TryGetValue(remoteEndPoint, out var connection);
            if (connection == null)
            {
                if (_connections.Count >= Config.MaximumConnections)
                {
                    return;
                }

                connection = new Connection(remoteEndPoint, this);
                if (_connections.TryAdd(remoteEndPoint, connection))
                {
                    connection.Connect();
                    NewConnection?.Invoke(this, connection);
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
