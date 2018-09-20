using Lure.Net.Data;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class ClientPeer : Peer
    {
        private readonly ClientPeerConfig _config;
        private readonly Connection _connection;

        public ClientPeer(string hostname, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new ClientPeerConfig
            {
                Hostname = hostname,
                Port = port,
                AddressFamily = addressFamily,
            })
        {
        }

        public ClientPeer(ClientPeerConfig config)
            : base(config)
        {
            _config = config;

            var hostAddress = NetHelper.ResolveAddress(_config.Hostname, _config.AddressFamily);
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve hostname '{_config.Hostname}'");
            }
            var remoteEndPoint = new IPEndPoint(hostAddress, _config.Port);
            _connection = new Connection(remoteEndPoint, this);
        }


        public new ClientPeerConfig Config => _config;

        public Connection Connection => _connection;


        protected override void OnUpdate()
        {
            _connection.Update();
        }


        internal override void OnDisconnect(Connection connection)
        {
            throw new System.NotImplementedException();
        }

        internal override void OnPacketReceived(IPEndPoint remoteEndPoint, byte channelId, INetDataReader reader)
        {
            if (_connection.RemoteEndPoint == remoteEndPoint)
            {
                _connection.OnReceivedPacket(channelId, reader);
            }
        }


        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
