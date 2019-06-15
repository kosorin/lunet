using Lunet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

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


        public void Run()
        {
            _socket.Bind();
        }


        private void Socket_PacketReceived(InternetEndPoint remoteEndPoint, IncomingProtocolPacket packet)
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

            connection.HandleIncomingPacket(packet);
        }

        private void Connection_Disconnected(Connection connection)
        {
            lock (_connectionsLock)
            {
                _connections.Remove(connection.RemoteEndPoint);
                connection.Disconnected -= Connection_Disconnected;
            }
        }


        private int _disposed;

        public virtual bool IsDisposed => _disposed == 1;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                lock (_connectionsLock)
                {
                    foreach (var connection in _connections.Values)
                    {
                        connection.Dispose();
                    }
                }
                _socket.Dispose();
            }
        }
    }
}
