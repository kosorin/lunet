using Serilog;
using System;
using System.Net;
using System.Net.Sockets;

namespace Bur.Net
{
    public abstract class NetPeer
    {
        private const int StopTimeout = 1; // 1 second
        private const int ReceiveBufferSize = 4 * 1024; // 4 kB

        private static readonly ILogger Logger = Log.ForContext<NetPeer>();

        protected volatile bool _isRunning;

        protected readonly AddressFamily _family;
        protected Socket _socket;
        protected IPEndPoint _localEndPoint;

        private byte[] _receiveBuffer = new byte[ReceiveBufferSize];


        protected NetPeer(AddressFamily family)
        {
            if (!AddressHelpers.ValidateAddressFamily(family))
            {
                throw new ArgumentException($"{nameof(NetConnection)} only accept {AddressFamily.InterNetwork} or {AddressFamily.InterNetworkV6} addresses.", nameof(family));
            }

            _family = family;
        }


        public bool IsRunning
        {
            get => _isRunning;
        }

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }
            _isRunning = true;

            _socket = CreateSocket() ?? throw new SocketException((int)SocketError.NotConnected);

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
                _socket.Close(StopTimeout);
                Logger.Debug("Peer stopped");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Stop peer");
            }
        }


        protected abstract Socket CreateSocket();

        protected virtual void OnStart()
        {
            BeginReceive();
        }


        private void BeginReceive()
        {
            var remoteEndPoint = (EndPoint)EndPointHelpers.GetAny(_socket.AddressFamily);
            _socket.BeginReceiveFrom(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ref remoteEndPoint, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!_isRunning)
            {
                return;
            }

            var remoteEndPoint = (EndPoint)EndPointHelpers.GetAny(_socket.AddressFamily);
            try
            {
                var size = _socket.EndReceiveFrom(ar, ref remoteEndPoint);
                Logger.Verbose("[{RemoteEndPoint}] Received data (size={Size})", remoteEndPoint, size);

                if (size > 0)
                {
                    var data = new byte[size];
                    Array.Copy(_receiveBuffer, 0, data, 0, size);
                }
                else
                {
                    Logger.Verbose("[{RemoteEndPoint}] Channel closed by a remote host", remoteEndPoint);
                    Stop();
                }
            }
            catch (SocketException e)
            {
                var isError = true;
                switch (e.SocketErrorCode)
                {
                case SocketError.MessageSize:
                    Logger.Warning("[{RemoteEndPoint}] Received data are too big (size>{ReceiveBufferSize})", remoteEndPoint, ReceiveBufferSize);
                    isError = false;
                    break;
                }

                if (isError)
                {
                    Logger.Error(e, "[{RemoteEndPoint}] Unable to receive data ({SocketErrorCode}:{ErrorCode})", remoteEndPoint, e.SocketErrorCode, e.ErrorCode);
                    Stop();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "[{RemoteEndPoint}] Unable to receive data", remoteEndPoint);
                Stop();
            }

            BeginReceive();
        }

    }
}
