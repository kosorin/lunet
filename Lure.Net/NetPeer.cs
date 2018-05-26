using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public abstract class NetPeer : IDisposable
    {
        /// <summary>
        /// Number of channels (Receive + Send).
        /// </summary>
        private const int SocketChannelCount = 2;

        private static readonly ILogger Logger = Log.ForContext<NetPeer>();

        private readonly NetPeerConfiguration _config;
        private readonly ObjectPool<SocketAsyncEventArgs> _tokenPool;
        private readonly TokenBufferManager _tokenBufferManager;
        private readonly Dictionary<IPEndPoint, NetConnection> _connections;
        private readonly Dictionary<uint, PacketData> _sendBuffer;

        private volatile bool _isRunning;
        private bool _disposed;
        private Socket _socket;

        internal NetPeer(NetPeerConfiguration config)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            _config = config;

            _tokenPool = new ObjectPool<SocketAsyncEventArgs>(SocketChannelCount, TokenFactory);
            _tokenBufferManager = new TokenBufferManager(SocketChannelCount, _config.PacketBufferSize);

            _connections = new Dictionary<IPEndPoint, NetConnection>();

            _sendBuffer = new Dictionary<uint, PacketData>();
        }


        public bool IsRunning => _isRunning;

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint;

        internal Socket Socket => _socket;


        public void SendMessage(NetConnection connection, INetMessage message)
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
                OnStop();

                _socket.Close(_config.CloseTimeout);
                Logger.Debug("Peer stopped");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Stop peer");
            }
            finally
            {
                _socket = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void OnStart()
        {
            StartReceive();
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
                    _tokenPool.Dispose();
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

        private SocketAsyncEventArgs TokenFactory()
        {
            var token = new SocketAsyncEventArgs();
            token.Completed += IO_Completed;
            token.RemoteEndPoint = _socket.AddressFamily.GetAnyEndPoint();
            _tokenBufferManager.SetBuffer(token);
            return token;
        }

        private void StartReceive()
        {
            StartReceive(_tokenPool.Rent());
        }

        private void StartReceive(SocketAsyncEventArgs token)
        {
            if (!_socket.ReceiveFromAsync(token))
            {
                if (!ProcessError(token))
                {
                    ProcessReceive(token);
                }
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (token.BytesTransferred > 0 && token.SocketError == SocketError.Success)
            {
                if (token.BytesTransferred >= 9)
                {
                    Logger.Verbose("[{RemoteEndPoint}] Received data (size={Size})", token.RemoteEndPoint, token.BytesTransferred);

                    var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                    var reader = new NetDataReader(token.Buffer, token.Offset, token.BytesTransferred);
                    var packet = new NetPacketHeader
                    {
                        Type = (NetPacketType)reader.ReadByte(),
                        Sequence = reader.ReadUShort(),
                        Ack = reader.ReadUShort(),
                        AckBits = reader.ReadUInt(),
                    };
                }

                StartReceive(token);
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

        private void StartSend()
        {
            StartSend(_tokenPool.Rent());
        }

        private void StartSend(SocketAsyncEventArgs token)
        {
            if (!_socket.SendToAsync(token))
            {
                ProcessSend(token);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            _tokenPool.Return(token);
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            if (!ProcessError(token))
            {
                return;
            }

            switch (token.LastOperation)
            {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive(token);
                break;

            case SocketAsyncOperation.Send:
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
