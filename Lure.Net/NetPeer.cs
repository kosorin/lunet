using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lure.Net
{
    public abstract class NetPeer : IDisposable
    {
        private const int FPS = 5;

        private static readonly ILogger Logger = Log.ForContext<NetPeer>();

        private readonly NetPeerConfiguration _config;
        private readonly Dictionary<IPEndPoint, NetConnection> _connections;

        private readonly PacketPool _packetPool;
        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly ObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        private volatile bool _isRunning;
        private bool _disposed;
        private Socket _socket;
        private Thread _thread;

        internal NetPeer(NetPeerConfiguration config)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            _config = config;

            _connections = new Dictionary<IPEndPoint, NetConnection>();

            _packetPool = new PacketPool();
            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(_config.MaxClients * 4, CreateSendToken);
        }

        public bool IsRunning => _isRunning;

        public long CurrentTimestamp { get; private set; }

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint;

        internal Socket Socket => _socket;


        public void SendMessage(NetConnection connection, NetMessage message)
        {
            if (connection.Peer != this)
            {
                throw new NetException();
            }
            connection.SendMessage(message);
        }

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }
            _isRunning = true;

            Logger.Verbose("Starting peer");

            BindSocket();
            StartReceive();
            StartLoop();

            OnStart();

            Logger.Debug("Peer started");
        }

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
                CloseSocket();

                OnStop();

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


        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _packetPool.Dispose();
                    _receiveToken.Dispose();
                    _sendTokenPool.Dispose();
                    _socket?.Close();
                }
                _disposed = true;
            }
        }

        protected void AddConnection(NetConnection connection)
        {
            _connections[connection.RemoteEndPoint] = connection;
        }


        private void BindSocket()
        {
            try
            {
                _socket = new Socket(_config.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                //_socket.Blocking = false;
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
            catch (Exception e)
            {
                throw new NetException("Could not bind socket.", e);
            }
        }

        private void CloseSocket()
        {
            _socket.Close(_config.CloseTimeout);
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
            _thread.Join(TimeSpan.FromSeconds(5));
        }

        private void Loop()
        {
            const int frame = 1000 / FPS;
            var sw = Stopwatch.StartNew();
            CurrentTimestamp = sw.ElapsedMilliseconds;
            while (_isRunning)
            {
                Update();

                var current = sw.ElapsedMilliseconds;
                var delta = (int)(current - CurrentTimestamp);
                if (delta <= frame)
                {
                    CurrentTimestamp += frame;
                    Thread.Sleep(frame - delta);
                }
                else
                {
                    CurrentTimestamp = current;
                }
            }
        }

        private void Update()
        {
            foreach (var connection in _connections.Values)
            {
                foreach (var payload in connection.GetQueuedPayloads())
                {
                    var token = _sendTokenPool.Rent();
                    var writer = (NetDataWriter)token.UserToken;

                    writer.Reset();
                    foreach (var payloadMessage in payload.Messages)
                    {
                        writer.WriteUShort(payloadMessage.Id);
                        writer.WriteBytes(payloadMessage.Data);
                        writer.PadBits();
                    }
                    writer.Flush();

                    var packet = (PayloadPacket)_packetPool.Rent(PacketType.Payload);
                    connection.PreparePacket(packet);
                    packet.Data = writer.GetBytes();

                    writer.Reset();
                    writer.WriteSerializable(packet);
                    writer.Flush();
                    writer.SetTokenBuffer(token);

                    _packetPool.Return(packet);

                    token.RemoteEndPoint = connection.RemoteEndPoint;
                    StartSend(token);
                }
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

        private void ProcessReceive()
        {
            var token = _receiveToken;
            if (!ProcessError(token))
            {
                return;
            }

            if (token.BytesTransferred > 0 && token.SocketError == SocketError.Success)
            {
                var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                var reader = new NetDataReader(token.Buffer, token.Offset, token.BytesTransferred);

                var packet = _packetPool.Rent((PacketType)reader.ReadByte());
                reader.ReadSerializable(packet);

                Logger.Verbose("[{RemoteEndPoint}] Received data (size={Size}): {Type} {Sequence}", token.RemoteEndPoint, token.BytesTransferred, packet.Type, packet.Sequence);

                ProcessPacket(packet);

                _packetPool.Return(packet);

                StartReceive();
            }
            else
            {
                var isError = true;
                switch (token.SocketError)
                {
                case SocketError.MessageSize:
                    Logger.Warning("[{RemoteEndPoint}] Received data are too big (size>{ReceiveBufferSize})", token.RemoteEndPoint, _config.PacketBufferSize);
                    token.SocketError = SocketError.Success;
                    isError = false;
                    break;

                case SocketError.Success:
                    isError = false;
                    break;
                }

                if (isError)
                {
                    Logger.Error("[{RemoteEndPoint}] Unable to receive data ({SocketErrorCode}:{ErrorCode})", token.RemoteEndPoint, token.SocketError, (int)token.SocketError);
                    Stop();
                }
            }
        }

        private void ProcessPacket(Packet packet)
        {
            switch (packet.Type)
            {
            case PacketType.Fragment:
                break;

            case PacketType.Payload:
                var payloadPacket = (PayloadPacket)packet;
                var reader = new NetDataReader(payloadPacket.Data);
                payloadPacket.
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

        private SocketAsyncEventArgs CreateSendToken()
        {
            var token = new SocketAsyncEventArgs
            {
                UserToken = new NetDataWriter(),
            };
            token.Completed += IO_Completed;
            return token;
        }

        private void StartSend(SocketAsyncEventArgs token)
        {
            if (!_isRunning)
            {
                return;
            }

            if (!_socket.SendToAsync(token))
            {
                ProcessSend(token);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            if (ProcessError(token))
            {
                // TODO: Finish send operation
                Logger.Verbose("[{RemoteEndPoint}] Sent data (size={Size})", token.RemoteEndPoint, token.BytesTransferred);
            }

            _sendTokenPool.Return(token);
        }

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

        private bool ProcessError(SocketAsyncEventArgs token)
        {
            if (token.SocketError == SocketError.Success)
            {
                return true;
            }
            if (token.SocketError == SocketError.OperationAborted)
            {
                return false;
            }

            return false;
        }


        private class PacketData
        {
            public bool IsAcked { get; set; }
        }
    }
}
