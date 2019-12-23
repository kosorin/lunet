using Lunet.Common;
using Lunet.Extensions;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lunet
{
    internal sealed class UdpSocket : IDisposable
    {
        private readonly IPEndPoint _localEndPoint;

        private readonly Socket _socket;
        private readonly ObjectPool<UdpPacket> _packetPool;

        public UdpSocket(IPEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;

            _socket = new Socket(_localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            _packetPool = new ObjectPool<UdpPacket>(CreatePacket, 8);
        }

        public UdpSocket(AddressFamily addressFamily) : this(addressFamily.GetAnyEndPoint())
        {
        }


        public UdpPacket RentPacket()
        {
            return _packetPool.Rent();
        }


        public void Bind()
        {
            _socket.Bind(_localEndPoint);

            ReceivePacket();
        }


        public event TypedEventHandler<UdpSocket, UdpPacket>? PacketReceived;

        private void ReceivePacket()
        {
            if (IsDisposed)
            {
                return;
            }

            UdpPacket packet;
            try
            {
                packet = RentPacket();
            }
            catch (ObjectDisposedException)
            {
                // It's ok - we don't want to receive packets anymore
                return;
            }

            StartReceive(packet);
        }

        private void StartReceive(UdpPacket packet)
        {
            try
            {
                packet.BeginReceive();

                if (_socket.ReceiveFromAsync(packet.Operation))
                {
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // It's ok - we don't want to receive packets anymore
                packet.Dispose();
                return;
            }
            catch
            {
                packet.Return();
                throw;
            }

            ProcessReceive(packet);
        }

        private void ProcessReceive(UdpPacket packet)
        {
            if (IsDisposed)
            {
                packet.Dispose();
                return;
            }

            ReceivePacket();

            try
            {
                if (packet.EndReceive())
                {
                    try
                    {
                        PacketReceived?.Invoke(this, packet);
                    }
                    catch
                    {
                        // What now?
                    }
                }
                else
                {
                    // Ignore bad receive
                }
            }
            finally
            {
                packet.Return();
            }
        }


        public void SendPacket(UdpPacket packet)
        {
            StartSend(packet);
        }

        private void StartSend(UdpPacket packet)
        {
            try
            {
                packet.BeginSend();

                if (_socket.SendToAsync(packet.Operation))
                {
                    return;
                }
            }
            catch (ObjectDisposedException)
            {
                // It's ok - we don't want to send packets anymore
                packet.Dispose();
                return;
            }
            catch
            {
                packet.Return();
                throw;
            }

            ProcessSend(packet);
        }

        private void ProcessSend(UdpPacket packet)
        {
            if (IsDisposed)
            {
                packet.Dispose();
                return;
            }

            try
            {
                packet.EndSend();
            }
            finally
            {
                packet.Return();
            }
        }


        private UdpPacket CreatePacket()
        {
            var packet = new UdpPacket(_localEndPoint.AddressFamily);
            packet.Operation.Completed += IO_Completed;
            return packet;
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs operation)
        {
            var packet = (UdpPacket)operation.UserToken;

            if (IsDisposed)
            {
                packet.Dispose();
                return;
            }

            switch (packet.Operation.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(packet);
                    return;
                case SocketAsyncOperation.SendTo:
                    ProcessSend(packet);
                    return;
                default:
                    throw new InvalidOperationException("Unexpected socket operation.");
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
                _packetPool.Dispose();
            }
        }
    }
}
