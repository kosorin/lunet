using Lure.Net.Channels;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;
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

        private PacketReceiver _packetReceiver;
        private PacketSender _packetSender;

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

        internal Socket Socket => _socket;


        public event TypedEventHandler<NetPeer, NetConnection> Connected;

        public event TypedEventHandler<NetConnection, NetMessage> MessageReceived;


        public void Start()
        {
            if (_state == NetPeerState.Starting || _state == NetPeerState.Running)
            {
                return;
            }
            else if (_state != NetPeerState.Unstarted)
            {
                throw new NetException("Peer is in wrong state.");
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
            if (_state == NetPeerState.Stopping || _state == NetPeerState.Stopped)
            {
                return;
            }
            else if (_state != NetPeerState.Running)
            {
                throw new NetException("Peer is in wrong state.");
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


        internal void OnReceivedPacket(IPEndPoint remoteEndPoint, byte channelId, INetDataReader reader)
        {
            _connections.TryGetValue(remoteEndPoint, out var connection);
            if (connection == null)
            {
                if (!Config.AcceptIncomingConnections)
                {
                    return;
                }

                var newConnection = new NetConnection(remoteEndPoint, this);
                if (_connections.TryAdd(remoteEndPoint, newConnection))
                {
                    Connected?.Invoke(this, newConnection);
                    connection = newConnection;
                }
            }

            if (connection != null)
            {
                connection.OnReceivedPacket(channelId, reader);
            }
        }

        internal void SendPacket(IPEndPoint remoteEndPoint, byte channelId, INetPacket packet)
        {
            _packetSender.Send(remoteEndPoint, channelId, packet);
        }

        internal void InjectConnection(NetConnection connection)
        {
            if (!_connections.TryAdd(connection.RemoteEndPoint, connection))
            {
                throw new NetException($"Could not inject connection. Connection with remote end point {connection.RemoteEndPoint} already exists.");
            }
        }


        private void Setup()
        {
            BindSocket();

            _packetReceiver = new PacketReceiver(this);
            _packetSender = new PacketSender(this);

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
                    connection.Dispose();
                }
                _connections.Clear();
                _connections = null;
            }

            if (_packetSender != null)
            {
                _packetSender.Dispose();
                _packetSender = null;
            }

            if (_packetReceiver != null)
            {
                _packetReceiver.Dispose();
                _packetReceiver = null;
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

                Log.Debug("Bind socket {LocalEndPoint}", _socket.LocalEndPoint);
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

            _packetReceiver.StartReceive();

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
                while (connection.ReceivedMessages.TryDequeue(out var message))
                {
                    MessageReceived?.Invoke(connection, message);
                }
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
