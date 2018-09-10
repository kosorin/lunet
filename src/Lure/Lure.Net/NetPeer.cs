using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lure.Net
{
    public abstract class NetPeer : IDisposable
    {
        private const int FPS = 60;

        private readonly NetPeerConfiguration _config;
        private volatile NetPeerState _state;

        private Socket _socket;
        private Thread _thread;

        private SocketAsyncEventArgs _receiveToken;
        private ObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        private ConcurrentDictionary<IPEndPoint, NetConnection> _connections;

        private protected NetPeer(NetPeerConfiguration config)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            _config = config;

            _state = NetPeerState.Unstarted;
        }

        public NetPeerConfiguration Config => _config;

        public NetPeerState State => _state;

        public bool IsRunning => _state == NetPeerState.Running;

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket?.LocalEndPoint;

        public IEnumerable<NetConnection> Connections => _connections.Values;


        public void Start()
        {
            if (_state != NetPeerState.Unstarted)
            {
                return;
            }

            _state = NetPeerState.Starting;
            Log.Verbose("Starting peer");

            try
            {
                Setup();

                _state = NetPeerState.Running;
                Log.Debug("Peer started");

                StartLoop();
            }
            catch
            {
                _state = NetPeerState.Error;
                throw;
            }
        }

        public void Stop()
        {
            if (_state != NetPeerState.Running)
            {
                return;
            }

            _state = NetPeerState.Stopping;
            Log.Verbose("Stopping peer");

            try
            {
                StopLoop();

                Cleanup();

                _state = NetPeerState.Stopped;
                Log.Debug("Peer stopped");
            }
            catch
            {
                _state = NetPeerState.Error;
                throw;
            }
        }


        protected abstract void OnSetup();

        protected abstract void OnCleanup();


        protected void InjectConnection(NetConnection connection)
        {
            if (!_connections.TryAdd(connection.RemoteEndPoint, connection))
            {
                throw new NetException($"Could not inject connection. Connection with remote end point {connection.RemoteEndPoint} already exists.");
            }
        }


        private void Setup()
        {
            BindSocket();

            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);

            _connections = new ConcurrentDictionary<IPEndPoint, NetConnection>();

            OnSetup();
        }

        private void Cleanup()
        {
            OnCleanup();

            if (_connections != null)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.Disconnect();
                    connection.Dispose();
                }
                _connections.Clear();
                _connections = null;
            }

            if (_receiveToken != null)
            {
                _receiveToken.Dispose();
                _receiveToken = null;
            }

            if (_sendTokenPool != null)
            {
                _sendTokenPool.Dispose();
                _sendTokenPool = null;
            }

            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
        }

        private void BindSocket()
        {
            try
            {
                _socket = new Socket(_config.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _socket.SendBufferSize = _config.SendBufferSize;
                _socket.ReceiveBufferSize = _config.ReceiveBufferSize;
                if (_config.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    _socket.DualMode = _config.DualMode;
                }

                var address = _config.AddressFamily.GetAnyAddress();
                var localEndPoint = new IPEndPoint(address, _config.LocalPort ?? IPEndPoint.MinPort);
                _socket.Bind(localEndPoint);
            }
            catch (SocketException e)
            {
                throw new NetException("Could not bind socket.", e);
            }
        }


        private void StartLoop()
        {
            _thread = new Thread(Loop)
            {
                Name = GetType().FullName,
                IsBackground = true,
            };
            _thread.Start();
        }

        private void StopLoop()
        {
            _thread.Join();
        }

        private void Loop()
        {
            const int frame = Time.MillisecondsPerSecond / FPS;

            StartReceive();

            var previous = Timestamp.Current;
            while (IsRunning)
            {
                Update();

                var current = Timestamp.Current;
                var diff = (int)(current - previous);
                if (diff <= frame)
                {
                    previous += frame;
                    Thread.Sleep(frame - diff);
                }
                else
                {
                    previous = current;
                }
            }
        }

        private void Update()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Update();
            }
        }


        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[_config.PacketBufferSize];
            var token = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _config.AddressFamily.GetAnyEndPoint(),
                UserToken = new NetDataReader(buffer),
            };
            token.Completed += IO_Completed;
            token.SetBuffer(buffer, 0, buffer.Length);
            return token;
        }

        private void StartReceive()
        {
            if (!IsRunning)
            {
                return;
            }

            // TODO: Is it necessary to reset remote end point every receive call?
            _receiveToken.RemoteEndPoint = _socket.AddressFamily.GetAnyEndPoint();
            if (!_socket.ReceiveFromAsync(_receiveToken))
            {
                ProcessReceive(_receiveToken);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (!IsRunning)
            {
                return;
            }

            if (token.IsOk())
            {
                var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                var reader = token.GetReader();
                OnPacketReceived(remoteEndPoint, reader);
            }

            StartReceive();
        }

        private void OnPacketReceived(IPEndPoint remoteEndPoint, INetDataReader reader)
        {
            NetConnection connection;
            if (Config.AcceptIncomingConnections)
            {
                connection = _connections.GetOrAdd(remoteEndPoint, x => new NetConnection(x, this));
            }
            else
            {
                _connections.TryGetValue(remoteEndPoint, out connection);
            }

            if (connection != null)
            {
                connection.ProcessIncomingPacket(reader);
            }
        }


        internal void SendPacket(NetConnection connection, INetPacket packet)
        {
            if (packet.Direction != NetPacketDirection.Outgoing)
            {
                throw new ArgumentException("Invalid packet direction.", nameof(packet));
            }

            var token = _sendTokenPool.Rent();

            var writer = (NetDataWriter)token.UserToken;
            try
            {
                writer.Reset();
                packet.SerializeHeader(writer);
                packet.SerializeData(writer);
                writer.Flush();
            }
            catch (NetSerializationException)
            {
                _sendTokenPool.Return(token);
                return;
            }

            token.SetWriter(writer);
            token.RemoteEndPoint = connection.RemoteEndPoint;

            StartSend(token);
        }

        private SocketAsyncEventArgs CreateSendToken()
        {
            var token = new SocketAsyncEventArgs
            {
                UserToken = new NetDataWriter(_config.PacketBufferSize),
            };
            token.Completed += IO_Completed;
            return token;
        }

        private void StartSend(SocketAsyncEventArgs token)
        {
            if (!IsRunning)
            {
                _sendTokenPool.Return(token);
                return;
            }

            if (!_socket.SendToAsync(token))
            {
                ProcessSend(token);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            _sendTokenPool.Return(token);
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            switch (token.LastOperation)
            {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive(token);
                break;

            case SocketAsyncOperation.SendTo:
                ProcessSend(token);
                break;

            default:
                throw new InvalidOperationException("Unexpected socket async operation.");
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
                    Cleanup();
                }
                _disposed = true;
            }
        }
    }
}
