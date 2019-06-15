using Lunet.Common;
using Lunet.Data;
using Lunet.Extensions;
using System;
using System.Net.Sockets;

namespace Lunet
{
    internal class UdpSocket : IDisposable
    {
        private readonly ProtocolProcessor _protocolProcessor = new ProtocolProcessor();
        private readonly InternetEndPoint _localEndPoint;

        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly ObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        public UdpSocket(InternetEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;

            _socket = new Socket(_localEndPoint.EndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);
        }

        public UdpSocket(IPVersion ipVersion) : this(new InternetEndPoint(ipVersion.ToAddressFamily().GetAnyEndPoint()))
        {
        }


        public bool IsDisposed => _disposed || _disposing;


        public void Bind()
        {
            try
            {
                _socket.Bind(_localEndPoint.EndPoint);

                StartReceive();
            }
            catch (SocketException e)
            {
                throw new NetException("Could not bind socket.", e);
            }
        }


        public event TypedEventHandler<InternetEndPoint, NetDataReader> PacketReceived;

        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[ushort.MaxValue];
            var token = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _socket.AddressFamily.GetAnyEndPoint(),
                UserToken = new NetDataReader(buffer),
            };
            token.Completed += IO_Completed;
            token.SetBuffer(buffer, 0, buffer.Length);
            return token;
        }

        private void StartReceive()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_socket.ReceiveFromAsync(_receiveToken))
            {
                return;
            }

            ProcessReceive(_receiveToken);
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (token.SocketError == SocketError.Success && token.BytesTransferred > 0)
            {
                var remoteEndPoint = new InternetEndPoint(token.RemoteEndPoint);
                var reader = (NetDataReader)token.UserToken;
                reader.Reset(token.Offset, token.BytesTransferred);

                PacketReceived?.Invoke(remoteEndPoint, reader);
            }
            else
            {
                // Ignore bad receive
            }

            StartReceive();
        }


        public void SendPacket(InternetEndPoint remoteEndPoint, byte channelId, IChannelPacket packet)
        {
            if (IsDisposed)
            {
                return;
            }

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
            token.RemoteEndPoint = remoteEndPoint.EndPoint;

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
                if (!_socket.SendToAsync(token))
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

        private void WritePacket(NetDataWriter writer, byte channelId, IChannelPacket packet)
        {
            writer.Reset();
            _protocolProcessor.Write(writer, channelId, packet);
            writer.Flush();
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            if (IsDisposed)
            {
                return;
            }

            switch (token.LastOperation)
            {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive(token);
                return;
            case SocketAsyncOperation.SendTo:
                ProcessSend(token);
                return;
            default:
                throw new InvalidOperationException("Unexpected socket async operation.");
            }
        }


        private bool _disposing;
        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposing = true;

            if (disposing)
            {
                _socket.Dispose();

                _receiveToken.Dispose();
                _sendTokenPool.Dispose();
            }

            _disposing = false;
            _disposed = true;
        }
    }
}
