using Lure.Collections;
using Lure.Net.Data;
using System;
using System.Net.Sockets;
using System.Threading;

namespace Lure.Net.Tcp
{
    internal class TcpSocket : IDisposable
    {
        private readonly ISocketConfig _config;

        private readonly ProtocolProcessor _protocolProcessor = new ProtocolProcessor();

        private readonly Socket _socket;
        private readonly AutoResetEvent _connectEvent = new AutoResetEvent(false);
        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly IObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        public TcpSocket(ISocketConfig config, InternetEndPoint remoteEndPoint)
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

            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);

            RemoteEndPoint = remoteEndPoint;
        }

        internal TcpSocket(ISocketConfig config, Socket socket)
        {
            _config = config;

            _socket = socket;

            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);

            RemoteEndPoint = new InternetEndPoint(_socket.RemoteEndPoint);

            StartReceive();
        }


        public InternetEndPoint RemoteEndPoint { get; }


        public void Close()
        {
            if (_socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            _socket.Close();
            _socket.Dispose();

            _connectEvent.Dispose();
            _receiveToken.Dispose();
            _sendTokenPool.Dispose();
        }


        public event TypedEventHandler<TcpSocket> Disconnected;

        public void Connect()
        {
            try
            {
                _connectEvent.Reset();

                var token = CreateConnectToken();
                if (!_socket.ConnectAsync(token))
                {
                    ProcessConnect();
                }

                _connectEvent.WaitOne();

                if (token.SocketError != SocketError.Success)
                {
                    throw new NetException("Could not connect.");
                }

                StartReceive();
            }
            catch (SocketException e)
            {
                throw new NetException("Could not connect.", e);
            }
        }

        private SocketAsyncEventArgs CreateConnectToken()
        {
            var token = new SocketAsyncEventArgs();
            token.RemoteEndPoint = RemoteEndPoint.EndPoint;
            token.Completed += IO_Completed;
            return token;
        }

        private void ProcessConnect()
        {
            _connectEvent.Set();
        }


        public event PacketReceivedHandler<InternetEndPoint> PacketReceived;

        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[ushort.MaxValue];
            var token = new SocketAsyncEventArgs();
            token.Completed += IO_Completed;
            token.SetBuffer(buffer, 0, buffer.Length);
            return token;
        }

        private void StartReceive()
        {
            try
            {
                if (!_socket.ReceiveAsync(_receiveToken))
                {
                    ProcessReceive(_receiveToken);
                }
            }
            catch (ObjectDisposedException) { }
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (token.SocketError != SocketError.Success || token.BytesTransferred <= 0)
            {
                Disconnected?.Invoke(this);
                Close();
            }

            ReadPacket(token);
            StartReceive();
        }

        private void ReadPacket(SocketAsyncEventArgs token)
        {
            var data = _protocolProcessor.Read(token.Buffer, token.Offset, token.BytesTransferred);
            if (data.Reader == null)
            {
                return;
            }

            PacketReceived?.Invoke(new InternetEndPoint(token.RemoteEndPoint), data.ChannelId, data.Reader);
        }


        public void SendPacket(byte channelId, IPacket packet)
        {
            var token = _sendTokenPool.Rent();

            var writer = (NetDataWriter)token.UserToken;
            try
            {
                WritePacket(writer, channelId, packet);
            }
            catch
            {
                _sendTokenPool.Return(token);
                return;
            }

            token.SetBuffer(writer.Data, writer.Offset, writer.Length);

            StartSend(token);
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
            try
            {
                if (!_socket.SendAsync(token))
                {
                    ProcessSend(token);
                }
            }
            catch (ObjectDisposedException)
            {
                _sendTokenPool.Return(token);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            _sendTokenPool.Return(token);
        }

        private void WritePacket(NetDataWriter writer, byte channelId, IPacket packet)
        {
            writer.Reset();
            _protocolProcessor.Write(writer, channelId, packet);
            writer.Flush();
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            switch (token.LastOperation)
            {
            case SocketAsyncOperation.Connect:
                ProcessConnect();
                break;
            case SocketAsyncOperation.Receive:
                ProcessReceive(token);
                break;
            case SocketAsyncOperation.Send:
                ProcessSend(token);
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
