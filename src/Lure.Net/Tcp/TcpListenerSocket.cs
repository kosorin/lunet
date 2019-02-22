using Lure.Collections;
using System;
using System.Net.Sockets;

namespace Lure.Net.Tcp
{
    internal class TcpListenerSocket : IDisposable
    {
        private readonly int _listenBacklog = 32;
        private readonly InternetEndPoint _localEndPoint;

        private readonly Socket _socket;
        private readonly IObjectPool<SocketAsyncEventArgs> _acceptTokenPool;

        public TcpListenerSocket(InternetEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;

            _socket = new Socket(_localEndPoint.EndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
                _socket.Bind(_localEndPoint.EndPoint);
                _socket.Listen(_listenBacklog);

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
                    AcceptSocket?.Invoke(this, new TcpSocket(token.AcceptSocket));
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
