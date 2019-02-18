using Lure.Net.Data;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class ClientPeer : Peer
    {
        private readonly ClientConfiguration _config;
        private readonly Connection _connection;

        public ClientPeer(string hostname, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new ClientConfiguration
            {
                Hostname = hostname,
                Port = port,
                AddressFamily = addressFamily,
            }, null)
        {
        }

        public ClientPeer(ClientConfiguration config, IChannelFactory channelFactory)
            : base(config, channelFactory)
        {
            _config = config;

            var hostAddress = NetHelper.ResolveAddress(_config.Hostname, _config.AddressFamily);
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve hostname '{_config.Hostname}'");
            }
            var remoteEndPoint = new IPEndPoint(hostAddress, _config.Port);
            _connection = new Connection(remoteEndPoint, this, ChannelFactory);
        }


        public new ClientConfiguration Config => _config;

        public Connection Connection => _connection;


        protected override void OnStart()
        {
            base.OnStart();
            _connection.OnConnect();
        }

        protected override void OnStop()
        {
            _connection.Disconnect();
            _connection.Dispose();
            base.OnStop();
        }

        protected override void OnUpdate()
        {
            _connection.Update();
        }


        internal override void Disconnect(Connection connection)
        {
            connection.OnDisconnect();
            connection.Dispose();
        }

        internal override void OnPacketReceived(IPEndPoint remoteEndPoint, byte channelId, NetDataReader reader)
        {
            if (_connection.RemoteEndPoint.Equals(remoteEndPoint))
            {
                _connection.OnReceivedPacket(channelId, reader);
            }
        }
    }
}
