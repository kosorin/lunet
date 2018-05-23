using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bur.Net
{
    public sealed class NetServer : NetPeer
    {
        private static readonly ILogger Logger = Log.ForContext<NetServer>();

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
    }
}
