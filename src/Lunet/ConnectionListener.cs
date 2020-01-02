using Lunet.Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Lunet
{
    public class ConnectionListener : IDisposable
    {
        private readonly UdpSocket _socket;

        private readonly Dictionary<UdpEndPoint, ServerConnection> _connections = new Dictionary<UdpEndPoint, ServerConnection>();
        private readonly object _connectionsLock = new object();
        private readonly ChannelSettings _channelSettings;

        public ConnectionListener(UdpEndPoint remoteEndPoint) : this(remoteEndPoint, ChannelSettings.Default)
        {
        }

        public ConnectionListener(UdpEndPoint localEndPoint, ChannelSettings channelSettings)
        {
            // TODO: new
            _socket = new UdpSocket(localEndPoint.EndPoint);
            _socket.PacketReceived += Socket_PacketReceived;
            _channelSettings = channelSettings;
        }


        public event TypedEventHandler<ConnectionListener, Connection>? NewConnection;

        private void OnNewConnection(Connection connection)
        {
            NewConnection?.Invoke(this, connection);
        }


        public void Run()
        {
            _socket.Bind();
        }


        private void Socket_PacketReceived(UdpSocket socket, UdpPacket packet)
        {
            ServerConnection? connection = null;

            lock (_connectionsLock)
            {
                _connections.TryGetValue(packet.RemoteEndPoint, out connection);
                if (connection == null)
                {
                    // TODO: new
                    connection = new ServerConnection(_socket, packet.RemoteEndPoint, _channelSettings);
                    _connections.Add(packet.RemoteEndPoint, connection);

                    connection.Disconnected += Connection_Disconnected;
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
