using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bur.Net
{
    public sealed class NetClient : NetPeer
    {
        private static readonly ILogger Logger = Log.ForContext<NetClient>();

        private readonly string _hostName;
        private readonly int _port;

        private NetConnection _connection;


        public NetClient(string hostName, int port, AddressFamily family = AddressFamily.InterNetwork)
            : base(family)
        {
            if (hostName == null)
            {
                throw new ArgumentNullException(nameof(hostName));
            }
            if (!EndPointHelpers.ValidatePortNumber(port))
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _hostName = hostName;
            _port = port;
        }


        public void SendMessage(string message)
        {
            _connection.SendMessage(message);
        }


        protected override Socket CreateSocket()
        {
            Socket socket = null;

            var addresses = Dns.GetHostAddresses(_hostName);
            foreach (var address in addresses.Where(x => x.AddressFamily == _family))
            {
                try
                {
                    socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    socket.Connect(address, _port);
                    Logger.Debug("Connect from {LocalEndPoint} to {RemoteEndPoint}", socket.LocalEndPoint, socket.RemoteEndPoint);
                    break;
                }
                catch
                {
                    if (socket != null)
                    {
                        socket.Close();
                        socket = null;
                    }
                }
            }

            return socket;
        }

        protected override void OnStart()
        {
            _connection = new NetConnection(_socket, (IPEndPoint)_socket.RemoteEndPoint);

            base.OnStart();
        }
    }
}
