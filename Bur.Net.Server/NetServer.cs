using Bur.Common;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Bur.Net.Server
{
    public abstract class NetServer : Runnable, INetServer
    {
        private static long lastClientId;

        private IConnectionListener connectionListener;

        protected NetServer()
        {
        }

        public ConcurrentDictionary<long, INetClient> Clients { get; } = new ConcurrentDictionary<long, INetClient>();

        public event TypedEventHandler<INetServer, NetClientConnectedEventArgs> ClientConnected;

        public event TypedEventHandler<INetServer, NetClientDisconnectedEventArgs> ClientDisconnected;

        public override void Start()
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;

            connectionListener = CreateConnectionListener();
            connectionListener.ChannelConnected += ConnectionListener_ChannelConnected;
            connectionListener.Start();
        }

        public override void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            IsRunning = false;

            if (connectionListener != null)
            {
                connectionListener.Stop();
                connectionListener.ChannelConnected -= ConnectionListener_ChannelConnected;
                connectionListener = null;
            }

            foreach (var client in Clients.Values.ToList())
            {
                client.Disconnect();
            }
            Clients.Clear();
        }

        protected abstract IConnectionListener CreateConnectionListener();

        protected virtual void OnClientConnected(INetClient client)
        {
            ClientConnected?.Invoke(this, new NetClientConnectedEventArgs(client));
        }

        protected virtual void OnClientDisconnected(INetClient client)
        {
            ClientDisconnected?.Invoke(this, new NetClientDisconnectedEventArgs(client));
        }

        private static long GetNewClientId()
        {
            return Interlocked.Increment(ref lastClientId);
        }

        private void ConnectionListener_ChannelConnected(object sender, ChannelConnectedEventArgs e)
        {
            var id = GetNewClientId();
            var channel = e.Channel;
            var client = new NetClient(id, channel);

            client.Disconnected += Client_Disconnected;

            Clients[client.Id] = client;
            OnClientConnected(client);

            channel.Start();
        }

        private void Client_Disconnected(INetClient client, EventArgs e)
        {
            Clients.TryRemove(client.Id, out var _);
            OnClientDisconnected(client);
        }
    }
}
