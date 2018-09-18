using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Packets;
using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    internal class PacketSender : IDisposable
    {
        private readonly NetPeer _peer;
        private readonly IObjectPool<SocketAsyncEventArgs> _tokenPool;

        public PacketSender(NetPeer peer)
        {
            _peer = peer;
            _tokenPool = new ObjectPool<SocketAsyncEventArgs>(CreateSendToken);
        }

        public void Send(IPEndPoint remoteEndPoint, byte channelId, INetPacket packet)
        {
            if (!_peer.IsRunning)
            {
                return;
            }

            var token = _tokenPool.Rent();

            var writer = (NetDataWriter)token.UserToken;
            try
            {
                writer.Reset();
                writer.WriteByte(channelId);
                packet.SerializeHeader(writer);
                packet.SerializeData(writer);
                writer.Flush();
            }
            catch (NetSerializationException)
            {
                _tokenPool.Return(token);
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
                UserToken = new NetDataWriter(_peer.Config.PacketBufferSize),
            };
            token.Completed += IO_Completed;
            return token;
        }

        private void StartSend(SocketAsyncEventArgs token)
        {
            if (!_peer.Socket.SendToAsync(token))
            {
                ProcessSend(token);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs token)
        {
            if (token.IsOk())
            {
                _peer.Statistics.SentBytes += (ulong)token.BytesTransferred;
                _peer.Statistics.SentPackets++;
            }
            _tokenPool.Return(token);
        }


        private void IO_Completed(object sender, SocketAsyncEventArgs token)
        {
            if (token.LastOperation == SocketAsyncOperation.SendTo)
            {
                ProcessSend(token);
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
                    _tokenPool.Dispose();
                }
                disposed = true;
            }
        }
    }
}
