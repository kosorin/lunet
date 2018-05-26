using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public class NetConnection
    {
        private static readonly ILogger Logger = Log.ForContext<NetConnection>();

        private readonly NetPeer _peer;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly ConcurrentQueue<INetMessage> _messageQueue;

        internal NetConnection(NetPeer peer, IPEndPoint remoteEndPoint)
        {
            _peer = peer;
            _remoteEndPoint = remoteEndPoint;
            _messageQueue = new ConcurrentQueue<INetMessage>();
        }

        internal NetPeer Peer => _peer;

        public IPEndPoint LocalEndPoint => _peer.LocalEndPoint;

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;

        internal ConcurrentQueue<INetMessage> MessageQueue => _messageQueue;


        public void SendMessage(INetMessage message)
        {
            _messageQueue.Enqueue(message);
        }
    }
}
