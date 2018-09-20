using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    internal delegate void PacketReceivedHandler(IPEndPoint remoteEndPoint, byte channelId, INetDataReader reader);

    internal sealed class SocketWrapper : IDisposable
    {
        private readonly ISocketConfig _config;

        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly IObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        private Socket _socket;

        public SocketWrapper(ISocketConfig config)
        {
            _config = config;

            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);
        }


        public event PacketReceivedHandler PacketReceived;


        public bool IsBound => _socket != null;


        public void Bind()
        {
            if (_socket != null)
            {
                throw new InvalidOperationException("Socket is already bound.");
            }

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
                Log.Debug("Bind socket {LocalEndPoint}", _socket.LocalEndPoint);

                StartReceive();
            }
            catch (SocketException e)
            {
                throw new NetException("Could not bind socket.", e);
            }
        }

        public void Unbind()
        {
            if (_socket == null)
            {
                return;
            }

            _socket.Dispose();
            _socket = null;
        }


        private void StartReceive()
        {
            if (_socket == null)
            {
                return;
            }

            try
            {
                // TODO: Is it necessary to reset remote end point every receive call?
                _receiveToken.RemoteEndPoint = _socket.AddressFamily.GetAnyEndPoint();
                if (!_socket.ReceiveFromAsync(_receiveToken))
                {
                    ProcessReceive(_receiveToken);
                }
            }
            catch (ObjectDisposedException) { }
        }

        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[_config.PacketBufferSize];
            var token = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _config.AddressFamily.GetAnyEndPoint(),
                UserToken = new NetDataReader(buffer),
            };
            token.Completed += IO_Completed;
            token.SetBuffer(buffer, 0, buffer.Length);
            return token;
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (_socket == null)
            {
                return;
            }

            if (token.IsOk())
            {
                //_peer.Statistics.ReceivedBytes += (ulong)token.BytesTransferred;
                //_peer.Statistics.ReceivedPackets++;

                var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                var reader = token.GetReader();
                var channelId = reader.ReadByte();
                PacketReceived?.Invoke(remoteEndPoint, channelId, reader);
            }

            StartReceive();
        }


        public void Send(IPEndPoint remoteEndPoint, byte channelId, INetPacket packet)
        {
            if (_socket == null)
            {
                return;
            }

            var token = _sendTokenPool.Rent();

            var writer = (NetDataWriter)token.UserToken;
            try
            {
                writer.Reset();
                writer.WriteByte(channelId);
                packet.SerializeHeader(writer);
                packet.SerializeData(writer);
                writer.Flush();
            }
            catch
            {
                _sendTokenPool.Return(token);
                return;
            }

            token.SetWriter(writer);
            token.RemoteEndPoint = remoteEndPoint;

            StartSend(token);
        }

        private SocketAsyncEventArgs CreateSendToken()
        {
            var token = new SocketAsyncEventArgs
            {
                UserToken = new NetDataWriter(_config.PacketBufferSize),
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
            catch (ObjectDisposedException) { }
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            if (_socket == null)
            {
                _sendTokenPool.Return(token);
                return;
            }

            if (token.IsOk())
            {
                //_peer.Statistics.SentBytes += (ulong)token.BytesTransferred;
                //_peer.Statistics.SentPackets++;
            }
            _sendTokenPool.Return(token);
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
                    Unbind();

                    _receiveToken.Dispose();
                    _sendTokenPool.Dispose();
                }
                disposed = true;
            }
        }
    }
}
