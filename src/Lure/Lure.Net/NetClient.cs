using Lure.Net.Messages;
using System;
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

        public NetConnection Connection => _connection;

        public IPEndPoint RemoteEndPoint => _connection?.RemoteEndPoint;


        public void Connect()
        {
            if (Connection.State == NetConnectionState.Disconnected)
            {
                var message = NetMessageManager.Create(SystemMessageType.ConnectionRequest);
                Connection.SendSystemMessage(message);
            }
        }

        public void Disconnect()
        {
            // TODO: Graceful client disconnect
            throw new NotImplementedException();
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
