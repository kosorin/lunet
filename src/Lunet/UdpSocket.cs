using Lunet.Common;
using Lunet.Data;
using Lunet.Extensions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lunet
{
    internal sealed class UdpSocket : IDisposable
    {
        private readonly InternetEndPoint _localEndPoint;
        private readonly IPEndPoint _localAnyIPEndPoint;

        private readonly Socket _socket;

        private readonly ObjectPool<SocketAsyncEventArgs> _receiveTokenPool;
        private readonly ObjectPool<SocketAsyncEventArgs> _sendTokenPool;

        public UdpSocket(InternetEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;
            _localAnyIPEndPoint = localEndPoint.EndPoint.AddressFamily.GetAnyEndPoint();

            _socket = new Socket(_localEndPoint.EndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            _receiveTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateReceiveToken);
            _sendTokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);
        }

        public UdpSocket(IPVersion ipVersion) : this(new InternetEndPoint(ipVersion.ToAddressFamily().GetAnyEndPoint()))
        {
        }


        public void Bind()
        {
            _socket.Bind(_localEndPoint.EndPoint);

            ReceivePacket();
        }


        public event TypedEventHandler<InternetEndPoint, IncomingProtocolPacket> PacketReceived;

        private void ReceivePacket()
        {
            if (IsDisposed)
            {
                return;
            }

            SocketAsyncEventArgs token;
            try
            {
                token = _receiveTokenPool.Rent();
            }
            catch (ObjectDisposedException)
            {
                // It's ok - we don't want to receive packets anymore
                return;
            }

            StartReceive(token);
        }

        private void StartReceive(SocketAsyncEventArgs token)
        {
            try
            {
                if (_socket.ReceiveFromAsync(token))
                {
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // It's ok - we don't want to receive packets anymore
                token.Dispose();
                return;
            }
            catch
            {
                _receiveTokenPool.Return(token);
                throw;
            }

            ProcessReceive(token);
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (IsDisposed)
            {
                token.Dispose();
                return;
            }

            ReceivePacket();

            try
            {
                if (token.SocketError == SocketError.Success && token.BytesTransferred > 0)
                {

                    var remoteEndPoint = new InternetEndPoint(token.RemoteEndPoint);
                    var packet = (IncomingProtocolPacket)token.UserToken;
                    if (packet.Read(token.Offset, token.BytesTransferred))
                    {
                        try
                        {
                            PacketReceived?.Invoke(remoteEndPoint, packet);
                        }
                        catch
                        {
                            // What now?
                        }
                    }
                }
                else
                {
                    // Ignore bad receive
                }
            }
            finally
            {
                _receiveTokenPool.Return(token);
            }
        }

        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[ushort.MaxValue];
            var token = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _localAnyIPEndPoint,
                UserToken = new IncomingProtocolPacket(new NetDataReader(buffer)),
            };
            token.Completed += IO_Completed;
            token.SetBuffer(buffer, 0, buffer.Length);
            return token;
        }


        public void SendPacket(InternetEndPoint remoteEndPoint, OutgoingProtocolPacket packet)
        {
            var token = _sendTokenPool.Rent();
            try
            {
                var writer = (NetDataWriter)token.UserToken;
                packet.Write(writer);
                token.SetBuffer(writer.Data, writer.Offset, writer.Length);
                token.RemoteEndPoint = remoteEndPoint.EndPoint;
            }
            catch
            {
                _sendTokenPool.Return(token);
                throw;
            }

            StartSend(token);
        }

        private void StartSend(SocketAsyncEventArgs token)
        {
            try
            {
                if (_socket.SendToAsync(token))
                {
                    return;
                }
            }
            catch
            {
                _sendTokenPool.Return(token);
                throw;
            }

            ProcessSend(token);
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            if (IsDisposed)
            {
                token.Dispose();
                return;
            }

            _sendTokenPool.Return(token);
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


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            if (IsDisposed)
            {
                token.Dispose();
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


        private int _disposed;

        public bool IsDisposed => _disposed == 1;

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                _socket.Dispose();

                _receiveTokenPool.Dispose();
                _sendTokenPool.Dispose();
            }
        }
    }
}
