using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net.Udp
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

        internal UdpSocket(AddressFamily addressFamily) : this(new InternetEndPoint(addressFamily.GetAnyAddress(), IPEndPoint.MinPort))
        {
        }


        public void Close()
        {
            _socket.Close();
            _socket.Dispose();

            _receiveToken.Dispose();
            _sendTokenPool.Dispose();
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


        public event UdpPacketReceivedHandler PacketReceived;

        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[ushort.MaxValue];
            var token = new SocketAsyncEventArgs();
            token.RemoteEndPoint = _socket.AddressFamily.GetAnyEndPoint();
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
            catch (ObjectDisposedException) { }
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (token.SocketError == SocketError.Success && token.BytesTransferred > 0)
            {
                PacketReceived?.Invoke(new InternetEndPoint(token.RemoteEndPoint), token.Buffer, token.Offset, token.BytesTransferred);
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
