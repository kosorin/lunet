﻿using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class NetServer : NetPeer
    {
        private readonly NetServerConfiguration _config;

        public NetServer(int localPort, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new NetServerConfiguration
            {
                LocalPort = localPort,
                AddressFamily = addressFamily,
            })
        {
        }

        public NetServer(NetServerConfiguration config)
            : base(config)
        {
            _config = config;
        }

        public new NetServerConfiguration Config => _config;


        protected override void OnSetup()
        {
        }

        protected override void OnCleanup()
        {
        }
    }
}
