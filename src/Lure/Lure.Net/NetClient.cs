using Lure.Net.Messages;
using System.Net;

namespace Lure.Net
{
    public sealed class NetClient : NetPeer
    {
        private readonly NetClientConfiguration _config;

        private NetConnection _connection;

        public NetClient(string hostname, int port)
            : this(new NetClientConfiguration
            {
                Hostname = hostname,
                Port = port,
            })
        {
        }

        public NetClient(NetClientConfiguration config)
            : base(config)
        {
            _config = config;
        }

        public NetConnection Connection => _connection;

        public IPEndPoint RemoteEndPoint => _connection?.RemoteEndPoint;


        public void Connect()
        {
            Start();
            if (!IsRunning)
            {
                return;
            }
        }

        public void Disconnect()
        {
            if (State != NetPeerState.Running)
            {
                return;
            }

            //Connection.Disconnect();
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

            _connection = new NetConnection(this, remoteEndPoint);

            InjectConnection(_connection);
        }

        protected override void OnCleanup()
        {
        }
    }
}
