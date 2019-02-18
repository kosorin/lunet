using Force.Crc32;
using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    internal sealed class SocketWrapper : IDisposable
    {
        private static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        private const uint Crc32Check = 0x2144DF1C; // dec = 558_161_692
        private const int Crc32Length = sizeof(uint);

        private readonly ISocketConfig _config;

        private readonly SocketAsyncEventArgs _receiveToken;
        private readonly IObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        private readonly uint _initialCrc32;
        private Socket _socket;
        private EndPoint _anyEndPoint;

        public SocketWrapper(ISocketConfig config)
        {
            _config = config;

            _receiveToken = CreateReceiveToken();
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);

            _initialCrc32 = Crc32Algorithm.Compute(Version.ToByteArray());
        }


        public event PacketReceivedHandler PacketReceived;


        public bool IsBound => _socket != null;

        public SocketStatistics Statistics { get; } = new SocketStatistics();


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

                _anyEndPoint = _socket.AddressFamily.GetAnyEndPoint();
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


        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[_config.PacketBufferSize];
            var token = new SocketAsyncEventArgs();
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
                _receiveToken.RemoteEndPoint = _anyEndPoint;
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

            if (IsOk(token))
            {
                Statistics.ReceivedBytes += (ulong)token.BytesTransferred;
                Statistics.ReceivedPackets++;

                var crc32 = Crc32Algorithm.Append(_initialCrc32, token.Buffer, token.Offset, token.BytesTransferred);
                if (crc32 != Crc32Check)
                {
                    StartReceive();
                    return;
                }

                var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                var reader = new NetDataReader(token.Buffer, token.Offset, token.BytesTransferred - Crc32Length);
                var channelId = reader.ReadByte();
                PacketReceived?.Invoke(remoteEndPoint, channelId, reader);
            }
            else
            {
                // Ignore bad receive
            }

            StartReceive();
        }


        public void Send(IPEndPoint remoteEndPoint, byte channelId, IPacket packet)
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

                var crc32 = Crc32Algorithm.Append(_initialCrc32, writer.Data, writer.Offset, writer.Length);
                writer.WriteUInt(crc32);
                writer.Flush();
            }
            catch
            {
                _sendTokenPool.Return(token);
                return;
            }

            token.SetBuffer(writer.Data, writer.Offset, writer.Length);
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

            if (IsOk(token))
            {
                Statistics.SentBytes += (ulong)token.BytesTransferred;
                Statistics.SentPackets++;
            }
            else
            {
                // Ignore bad send
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


        public static bool IsOk(SocketAsyncEventArgs token)
        {
            return token.SocketError == SocketError.Success && token.BytesTransferred > 0;
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
