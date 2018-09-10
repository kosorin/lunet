using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class NetClient : NetPeer
    {
        private readonly NetClientConfiguration _config;

        private NetConnection _connection;

        public NetClient(string hostname, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new NetClientConfiguration
            {
                Hostname = hostname,
                Port = port,
                AddressFamily = addressFamily,
            })
        {
        }

        public NetClient(NetClientConfiguration config)
            : base(config)
        {
            _config = config;
        }

        public new NetClientConfiguration Config => _config;

        public NetConnection Connection => _connection;


        public void Connect()
        {
            Start();
            Connection.Connect();
        }

        public void Disconnect()
        {
            Connection.Disconnect();
            Stop();
        }

        protected override void OnSetup()
        {
            var hostAddress = NetHelper.ResolveAddress(_config.Hostname, _config.AddressFamily);
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve a hostname '{_config.Hostname}'");
            }
            var remoteEndPoint = new IPEndPoint(hostAddress, _config.Port);

            _connection = new NetConnection(remoteEndPoint, this);

            InjectConnection(_connection);
        }

        protected override void OnCleanup()
        {
        }
    }
}
