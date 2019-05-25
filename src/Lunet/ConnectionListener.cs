﻿using Lunet.Common;
using System;
using System.Collections.Generic;

namespace Lunet
{
    public class ConnectionListener : IDisposable
    {
        private readonly UdpSocket _socket;

        private readonly Dictionary<InternetEndPoint, ServerConnection> _connections = new Dictionary<InternetEndPoint, ServerConnection>();
        private readonly object _connectionsLock = new object();
        private readonly IChannelFactory _channelFactory;

        public ConnectionListener(InternetEndPoint localEndPoint, IChannelFactory channelFactory)
        {
            _socket = new UdpSocket(localEndPoint);
            _socket.PacketReceived += Socket_PacketReceived;
            _channelFactory = channelFactory;
        }


        public event TypedEventHandler<ConnectionListener, Connection> NewConnection;

        private void OnNewConnection(Connection connection)
        {
            NewConnection?.Invoke(this, connection);
        }


        public void Start()
        {
            _socket.Bind();
        }

        public void Stop()
        {
            lock (_connectionsLock)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.Disconnect();
                }
            }
            _socket.Close();
        }


        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, byte[] data, int offset, int length)
        {
            ServerConnection? connection = null;

            lock (_connectionsLock)
            {
                _connections.TryGetValue(remoteEndPoint, out connection);
                if (connection == null)
                {
                    connection = new ServerConnection(_socket, remoteEndPoint, _channelFactory);
                    connection.Disconnected += Connection_Disconnected;
                    _connections.Add(remoteEndPoint, connection);

                    OnNewConnection(connection);
                }
            }

            connection.HandleReceivedPacket(data, offset, length);
        }

        private void Connection_Disconnected(Connection connection)
        {
            lock (_connectionsLock)
            {
                _connections.Remove(connection.RemoteEndPoint);
                connection.Disconnected -= Connection_Disconnected;
            }
        }


        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                }
                _disposed = true;
            }
        }
    }
}
