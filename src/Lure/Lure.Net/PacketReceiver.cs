using Lure.Net.Data;
using Lure.Net.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    internal class PacketReceiver : IDisposable
    {
        private readonly NetPeer _peer;
        private readonly SocketAsyncEventArgs _token;

        public PacketReceiver(NetPeer peer)
        {
            _peer = peer;
            _token = CreateReceiveToken();

            StartReceive();
        }

        public void StartReceive()
        {
            if (!_peer.IsRunning)
            {
                return;
            }

            // TODO: Is it necessary to reset remote end point every receive call?
            _token.RemoteEndPoint = _peer.Socket.AddressFamily.GetAnyEndPoint();
            if (!_peer.Socket.ReceiveFromAsync(_token))
            {
                ProcessReceive(_token);
            }
        }


        private SocketAsyncEventArgs CreateReceiveToken()
        {
            var buffer = new byte[_peer.Config.PacketBufferSize];
            var token = new SocketAsyncEventArgs
            {
                RemoteEndPoint = _peer.Config.AddressFamily.GetAnyEndPoint(),
                UserToken = new NetDataReader(buffer),
            };
            token.Completed += IO_Completed;
            token.SetBuffer(buffer, 0, buffer.Length);
            return token;
        }

        private void ProcessReceive(SocketAsyncEventArgs token)
        {
            if (!_peer.IsRunning)
            {
                return;
            }

            if (token.IsOk())
            {
                var remoteEndPoint = (IPEndPoint)token.RemoteEndPoint;
                var reader = token.GetReader();
                var channelId = reader.ReadByte();
                _peer.OnReceivedPacket(remoteEndPoint, channelId, reader);
            }

            StartReceive();
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            if (token.LastOperation == SocketAsyncOperation.ReceiveFrom)
            {
                ProcessReceive(token);
            }
            else
            {
                throw new InvalidOperationException("Unexpected socket async operation.");
            }
        }


        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _token.Dispose();
                }
                disposed = true;
            }
        }
    }
}
