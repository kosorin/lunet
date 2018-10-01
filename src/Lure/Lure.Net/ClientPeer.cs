﻿using Lure.Net.Data;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public sealed class ClientPeer : Peer
    {
        private readonly ClientPeerConfig _config;
        private readonly Connection _connection;

        public ClientPeer(string hostname, int port, AddressFamily addressFamily = AddressFamily.InterNetwork)
            : this(new ClientPeerConfig
            {
                Hostname = hostname,
                Port = port,
                AddressFamily = addressFamily,
            })
        {
        }

        public ClientPeer(ClientPeerConfig config)
            : base(config)
        {
            _config = config;

            var hostAddress = NetHelper.ResolveAddress(_config.Hostname, _config.AddressFamily);
            if (hostAddress == null)
            {
                throw new NetException($"Could not resolve hostname '{_config.Hostname}'");
            }
            var remoteEndPoint = new IPEndPoint(hostAddress, _config.Port);
            _connection = new Connection(remoteEndPoint, this);
        }


        public new ClientPeerConfig Config => _config;

        public Connection Connection => _connection;


        protected override void OnStop()
        {
            _connection.Disconnect();
            base.OnStop();
        }

        protected override void OnUpdate()
        {
            _connection.Update();
        }


        internal override void OnConnect(Connection connection)
        {
            if (!IsRunning)
            {
                throw new NetException("Peer is not ruuning.");
            }
            connection.OnConnect();
        }

        internal override void OnDisconnect(Connection connection)
        {
            connection.OnDisconnect();
        }

        internal override void OnPacketReceived(IPEndPoint remoteEndPoint, byte channelId, NetDataReader reader)
        {
            if (_connection.RemoteEndPoint.Equals(remoteEndPoint))
            {
                _connection.OnReceivedPacket(channelId, reader);
            }
        }
    }
}
