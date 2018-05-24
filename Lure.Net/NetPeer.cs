using Serilog;
using System;
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
        private readonly SocketBufferManager _bufferManager;
        private volatile bool _isRunning;
        private bool _disposed;
        private Socket _socket;

        protected NetPeer(NetPeerConfiguration config)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            _config = config;

            _tokenPool = new ObjectPool<SocketAsyncEventArgs>(SocketChannelCount, TokenFactory);
            _bufferManager = new SocketBufferManager(SocketChannelCount, _config.PacketBufferSize);
        }


        public bool IsRunning => _isRunning;

        public IPEndPoint LocalEndPoint => (IPEndPoint)_socket.LocalEndPoint;

        protected Socket Socket => _socket;


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
            _bufferManager.SetBuffer(token);
            return token;
        }

        private void StartReceive()
        {
            StartReceive(_tokenPool.Rent());
        }

        private void StartReceive(SocketAsyncEventArgs args)
        {
            if (!_socket.ReceiveFromAsync(args))
            {
                ProcessReceive(args);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                Logger.Verbose("[{RemoteEndPoint}] Received data (size={Size})", args.RemoteEndPoint, args.BytesTransferred);

                StartReceive(args);
            }
            else
            {
                var isError = true;
                switch (args.SocketError)
                {
                case SocketError.MessageSize:
                    Logger.Warning("[{RemoteEndPoint}] Received data are too big (size>{ReceiveBufferSize})", args.RemoteEndPoint, _config.PacketBufferSize);
                    args.SocketError = SocketError.Success;
                    isError = false;
                    break;

                case SocketError.Success:
                    isError = false;
                    break;
                }

                if (isError)
                {
                    Logger.Error("[{RemoteEndPoint}] Unable to receive data ({SocketErrorCode}:{ErrorCode})", args.RemoteEndPoint, args.SocketError, (int)args.SocketError);
                    Stop();
                }
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.OperationAborted)
            {
                return;
            }

            switch (args.LastOperation)
            {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive(args);
                break;

            case SocketAsyncOperation.Send:
                //this.ProcessSend(e);
                break;

            default:
                throw new InvalidOperationException("Unexpected socket async operation.");
            }
        }
    }
}
