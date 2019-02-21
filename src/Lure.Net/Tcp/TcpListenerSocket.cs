using Lure.Collections;
using Lure.Net.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net.Tcp
{
    internal class TcpListenerSocket : IDisposable
    {
        private readonly ISocketConfig _config;

        private readonly Socket _socket;
        private readonly IObjectPool<SocketAsyncEventArgs> _acceptTokenPool;

        public TcpListenerSocket(ISocketConfig config)
        {
            _config = config;

            _socket = new Socket(_config.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.SendBufferSize = _config.SendBufferSize;
            _socket.ReceiveBufferSize = _config.ReceiveBufferSize;
            _socket.ExclusiveAddressUse = false;
            if (_config.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _socket.DualMode = _config.DualMode;
            }

            _acceptTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateAcceptToken);
        }


        public void Close()
        {
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            _socket.Close();
            _socket.Dispose();

            _acceptTokenPool.Dispose();
        }


        public void Listen()
        {
            try
            {
                var address = _config.AddressFamily.GetLoopbackAddress();
                var localEndPoint = new IPEndPoint(address, _config.LocalPort ?? IPEndPoint.MinPort);
                _socket.Bind(localEndPoint);

                _socket.Listen(8); // TODO pending connections

                StartAccept();
            }
            catch (SocketException e)
            {
                throw new NetException("Could not listen.", e);
            }
        }

        public event TypedEventHandler<TcpListenerSocket, TcpSocket> AcceptSocket;

        private SocketAsyncEventArgs CreateAcceptToken()
        {
            var token = new SocketAsyncEventArgs();
            token.Completed += IO_Completed;
            return token;
        }

        private void StartAccept()
        {
            var token = _acceptTokenPool.Rent();

            try
            {
                token.AcceptSocket = null;
                if (!_socket.AcceptAsync(token))
                {
                    ProcessAccept(token);
                }
            }
            catch
            {
                _acceptTokenPool.Return(token);
                return;
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs token)
        {
            try
            {
                if (token.SocketError == SocketError.Success)
                {
                    AcceptSocket?.Invoke(this, new TcpSocket(_config, token.AcceptSocket));
                }
                else
                {
                    // TODO
                }
            }
            finally
            {
                _acceptTokenPool.Return(token);

                StartAccept();
            }
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            switch (token.LastOperation)
            {
            case SocketAsyncOperation.Accept:
                ProcessAccept(token);
                break;
            default:
                throw new InvalidOperationException("Unexpected socket async operation.");
            }
        }


        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Close();
                }
                disposed = true;
            }
        }
    }
}
