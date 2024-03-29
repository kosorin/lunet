﻿using Microsoft.Extensions.Logging;
using System.Threading;

namespace Lunet
{

    public class ClientConnection : Connection
    {
        private readonly UdpSocket _socket;

        internal ClientConnection(UdpEndPoint remoteEndPoint, ChannelFactory channelFactory, ILogger logger) : base(remoteEndPoint, channelFactory, logger)
        {
            // TODO: new
            _socket = new UdpSocket(remoteEndPoint.EndPoint.AddressFamily);
            _socket.PacketReceived += Socket_PacketReceived;

            State = ConnectionState.Disconnected;
        }


        public override void Connect()
        {
            State = ConnectionState.Connecting;
            _socket.Bind();

            State = ConnectionState.Connected;
        }


        private protected override void SendPacket(UdpPacket packet)
        {
            _socket.SendPacket(packet);
        }

        private protected override UdpPacket RentPacket()
        {
            var packet = _socket.RentPacket();
            packet.RemoteEndPoint = RemoteEndPoint;
            return packet;
        }


        private void Socket_PacketReceived(UdpSocket socket, UdpPacket packet)
        {
            if (RemoteEndPoint == packet.RemoteEndPoint)
            {
                HandleIncomingPacket(packet);
            }
        }


        private int _disposed;

        protected override void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                State = ConnectionState.Disconnecting;
                _socket.Dispose();

                State = ConnectionState.Disconnected;
                OnDisconnected();
            }

            base.Dispose(disposing);
        }
    }
}
