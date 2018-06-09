using Lure.Net.Messages;
using Serilog;
using System.Linq;
using System.Net;

namespace Lure.Net
{
    public sealed class NetClient : NetPeer
    {
        private static readonly ILogger Logger = Log.ForContext<NetClient>();

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

        public IPEndPoint RemoteEndPoint => Connection.RemoteEndPoint;


        public void SendMessage(NetMessage message)
        {
            SendMessage(_connection, message);
        }

        protected override void OnStart()
        {
            var hostAddress = NetHelper.ResolveAddress(_config.Hostname, _config.AddressFamily);
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve a hostname '{_config.Hostname}'");
            }
            var remoteEndPoint = new IPEndPoint(hostAddress, _config.Port);

            _connection = new NetConnection(this, remoteEndPoint);
            AddConnection(_connection);

            base.OnStart();
        }
    }
}
