using Serilog;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class NetServer : NetPeer
    {
        private readonly NetServerConfiguration _config;

        public NetServer(int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new NetServerConfiguration
            {
                LocalPort = port,
                AddressFamily = addressFamily,
            })
        {
        }

        public NetServer(NetServerConfiguration config)
            : base(config)
        {
            _config = config;
        }

        public override bool IsServer => true;
    }
}
