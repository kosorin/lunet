using Lunet.Common;
using Lunet.Common.Collections;
using Lunet.Data;
using Lunet.Extensions;
using System;
using System.Linq;
using System.Net.Sockets;

namespace Lunet
{
    internal class UdpSocket : IDisposable
    {
        private readonly ProtocolProcessor _protocolProcessor = new ProtocolProcessor();
        private readonly InternetEndPoint _localEndPoint;

        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly IObjectPool<SocketAsyncEventArgs> _sendTokenPool;

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


        public void Close()
        {
            Dispose();
        }

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
            try
            {
                if (!_socket.ReceiveFromAsync(_receiveToken))
                {
                    ProcessReceive(_receiveToken);
                }
            }
            catch (ObjectDisposedException)
            {
                // Guess it's ok...
            }
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
            switch (token.LastOperation)
            {
            case SocketAsyncOperation.ReceiveFrom:
                ProcessReceive(token);
                break;
            case SocketAsyncOperation.SendTo:
                ProcessSend(token);
                break;
            default:
                throw new InvalidOperationException("Unexpected socket async operation.");
            }
        }


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

            if (disposing)
            {
                _socket.Close();
                _socket.Dispose();

                _receiveToken.Dispose();
                _sendTokenPool.Dispose();
            }
            _disposed = true;
        }
    }
}
