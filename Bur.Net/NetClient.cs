using Serilog;
using System.Linq;
using System.Net;

namespace Bur.Net
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


        public void SendMessage(string message)
        {
            _connection.SendMessage(message);
        }

        protected override void OnStart()
        {
            IPAddress hostAddress;
            if (!IPAddress.TryParse(_config.Hostname, out hostAddress))
            {
                hostAddress = Dns
                    .GetHostAddresses(_config.Hostname)
                    .FirstOrDefault(x => x.AddressFamily == _config.AddressFamily);
            }

            if (hostAddress == null)
            {
            }

            var remoteEndPoint = new IPEndPoint(hostAddress, _config.Port);
            _connection = new NetConnection(Socket, remoteEndPoint);

            base.OnStart();
        }
    }
}
