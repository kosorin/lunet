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
        private readonly ISocketConfig _config;

        private readonly ProtocolProcessor _protocolProcessor = new ProtocolProcessor();

        private readonly Socket _socket;
        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly IObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        public UdpSocket(ISocketConfig config)
        {
            _config = config;

            _socket = new Socket(_config.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _socket.SendBufferSize = _config.SendBufferSize;
            _socket.ReceiveBufferSize = _config.ReceiveBufferSize;
            if (_config.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _socket.DualMode = _config.DualMode;
            }

            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);
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
                var address = _config.AddressFamily.GetLoopbackAddress();
                var localEndPoint = new IPEndPoint(address, _config.LocalPort ?? IPEndPoint.MinPort);
                _socket.Bind(localEndPoint);

                StartReceive();
            }
            catch (SocketException e)
            {
                throw new NetException("Could not bind socket.", e);
            }
        }


        public event PacketReceivedHandler<InternetEndPoint> PacketReceived;

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
            if (_socket == null)
            {
                return;
            }

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
            if (_socket == null)
            {
                return;
            }

            if (token.SocketError == SocketError.Success && token.BytesTransferred > 0)
            {
                ReadPacket(token);
            }
            else
            {
                // Ignore bad receive
            }

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


        public void SendPacket(InternetEndPoint remoteEndPoint, byte channelId, IPacket packet)
        {
            if (_socket == null)
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
            if (_socket == null)
            {
                _sendTokenPool.Return(token);
                return;
            }

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
