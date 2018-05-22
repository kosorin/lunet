using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bur.Net
{
    public sealed class NetServer : NetPeer
    {
        private static readonly ILogger Logger = Log.ForContext<NetServer>();

        private readonly int _port;


        public NetServer(int port, AddressFamily family = AddressFamily.InterNetwork)
            : base(family)
        {
            if (!EndPointHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _port = port;
        }


        protected override Socket CreateSocket()
        {
            var address = AddressHelpers.GetAny(_family);
            var localEndPoint = new IPEndPoint(address, _port);

            var socket = new Socket(_family, SocketType.Dgram, ProtocolType.Udp);

            if (_family == AddressFamily.InterNetworkV6)
            {
                socket.DualMode = true;
            }

            socket.Bind(localEndPoint);

            return socket;
        }
    }
}
