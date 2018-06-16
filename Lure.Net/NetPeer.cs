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
        private const int FPS = 5;
        private const int MillisecondsPerSecond = 1000;

        private static readonly ILogger Logger = Log.ForContext<NetPeer>();

        private readonly NetPeerConfiguration _config;

        private volatile bool _isRunning;
        private Thread _thread;
        private bool _disposed;

        private PacketManager _packetManager;
        private SocketAsyncEventArgs _receiveToken;
        private ObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        private Socket _socket;
        private ConcurrentDictionary<IPEndPoint, NetConnection> _connections;

        private protected NetPeer(NetPeerConfiguration config)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            _config = config;
        }

        public bool IsRunning => _isRunning;

        public abstract bool IsServer { get; }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint;


        // Thread: Main
        public void Start()
        {
            if (_isRunning)
            {
                return;
            }
            _isRunning = true;

            Logger.Verbose("Starting peer");

            Setup();
            OnStart();
            StartLoop();

            Logger.Debug("Peer started");
        }

        // Thread: -
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;

            Logger.Verbose("Stopping peer");

            try
            {
                StopLoop();
                OnStop();
                Cleanup();

                Logger.Debug("Peer stopped");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Stop peer");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }


        // Thread: Loop
        internal void SendPacket(NetConnection connection, Packet packet)
        {
            var token = _sendTokenPool.Rent();
            var writer = (NetDataWriter)token.UserToken;

            writer.Reset();
            writer.WriteSerializable(packet);
            writer.Flush();

            token.SetWriter(writer);
            token.RemoteEndPoint = connection.RemoteEndPoint;
            StartSend(token);

            Logger.Verbose("[{RemoteEndPoint}] Send data (size={Size}): {Type} {Seq}", connection.RemoteEndPoint, writer.Length, packet.Type, packet.Seq);
        }


        // Thread: Main
        protected virtual void OnStart()
        {
        }

        // Thread: -
        protected virtual void OnStop()
        {
        }

        // Thread: -
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

        // Thread: -
        protected NetConnection GetConnection(IPEndPoint remoteEndPoint)
        {
            if (!_connections.TryGetValue(remoteEndPoint, out var connection))
            {
                connection = new NetConnection(this, remoteEndPoint);
                _connections[remoteEndPoint] = connection;
            }

            return connection;
        }


        private void Setup()
        {
            _packetManager = new PacketManager();
            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(_config.MaxClients * 4, CreateSendToken);
            _connections = new ConcurrentDictionary<IPEndPoint, NetConnection>();

            BindSocket();
        }

        private void Cleanup()
        {
            if (_connections != null)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.Dispose();
                }
                _connections = null;
            }

            if (_packetManager != null)
            {
                _packetManager.Dispose();
                _packetManager = null;
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


        // Thread: Main
        private void StartLoop()
        {
            _thread = new Thread(Loop)
            {
                Name = GetType().FullName,
                IsBackground = true,
            };
            _thread.Start();
        }

        // Thread: -
        private void StopLoop()
        {
            _thread.Join();
        }

        // Thread: Loop
        private void Loop()
        {
            const int frame = MillisecondsPerSecond / FPS;

            StartReceive();

            var previous = Timestamp.Current;
            while (_isRunning)
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

        // Thread: Loop
        private void Update()
        {
            foreach (var connection in _connections.Values)
            {
                connection.Update();
            }
        }


        // Thread: Main
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

        // Thread: Loop, IOCP
        private void StartReceive()
        {
            if (!_isRunning)
            {
                return;
            }

            _receiveToken.RemoteEndPoint = _socket.AddressFamily.GetAnyEndPoint();
            if (!_socket.ReceiveFromAsync(_receiveToken))
            {
                ProcessReceive();
            }
        }

        // Thread: Loop, IOCP
        private void ProcessReceive()
        {
            if (!_isRunning)
            {
                return;
            }

            var token = _receiveToken;
            if (ProcessError(token))
            {
                var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                var connection = GetConnection(remoteEndPoint);

                var reader = token.GetReader();
                var packet = _packetManager.Parse(reader);
                if (packet != null)
                {
                    Logger.Verbose("[{RemoteEndPoint}] Received data (size={Size}): {Type} {Seq}", token.RemoteEndPoint, token.BytesTransferred, packet.Type, packet.Seq);

                    connection.AckReceive(packet.Seq);
                    connection.AckSend(packet.Ack, packet.AckBuffer);

                    ProcessPacket(packet);

                    _packetManager.Return(packet);
                }
            }

            StartReceive();
        }

        // Thread: Loop, IOCP
        private void ProcessPacket(Packet packet)
        {
            if (!_isRunning)
            {
                return;
            }

            switch (packet.Type)
            {
            case PacketType.Fragment:
                break;

            case PacketType.Payload:
                var payloadPacket = (PayloadPacket)packet;
                var reader = new NetDataReader(payloadPacket.Data);

                while (reader.Position < reader.Length)
                {
                    var seq = reader.ReadSeqNo();
                    var message = NetMessageManager.Create(reader.ReadUShort());
                    if (message == null)
                    {
                        return;
                    }
                    reader.ReadSerializable(message);
                    reader.PadBits();

                    Logger.Debug("  {Seq}: {Message}", seq, message);
                }
                break;

            case PacketType.Ping:
                break;

            case PacketType.Pong:
                break;

            case PacketType.ConnectRequest:
                break;

            case PacketType.ConnectAccept:
                break;

            case PacketType.ConnectDeny:
                break;

            case PacketType.KeepAlive:
                break;

            case PacketType.Disconnect:
                break;
            }
        }


        // Thread: Loop, IOCP
        private SocketAsyncEventArgs CreateSendToken()
        {
            var token = new SocketAsyncEventArgs
            {
                UserToken = new NetDataWriter(_config.PacketBufferSize),
            };
            token.Completed += IO_Completed;
            return token;
        }

        // Thread: Loop
        private void StartSend(SocketAsyncEventArgs token)
        {
            if (_isRunning)
            {
                if (!_socket.SendToAsync(token))
                {
                    ProcessSend(token);
                }
            }
        }

        // Thread: Loop, IOCP
        private void ProcessSend(SocketAsyncEventArgs token)
        {
            if (_isRunning)
            {
                if (ProcessError(token))
                {
                    // TODO: Finish send operation
                }
            }

            _sendTokenPool.Return(token);
        }


        // Thread: IOCP
        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            switch (token.LastOperation)
            {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive();
                break;

            case SocketAsyncOperation.SendTo:
                ProcessSend(token);
                break;

            default:
                throw new InvalidOperationException("Unexpected socket async operation.");
            }
        }

        // Thread: Loop, IOCP
        private bool ProcessError(SocketAsyncEventArgs token)
        {
            if (token.SocketError == SocketError.Success)
            {
                if (token.BytesTransferred <= 0)
                {
                    return false;
                }
                return true;
            }
            if (token.SocketError == SocketError.OperationAborted)
            {
                return false;
            }
            return false;
        }
    }
}
