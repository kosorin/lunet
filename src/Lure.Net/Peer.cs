using Lure.Net.Data;
using Lure.Net.Logging;
using System;
using System.Net;

namespace Lure.Net
{
    public abstract class Peer : IDisposable
    {
        private readonly SocketWrapper _socket;

        private volatile PeerState _state;

        private protected Peer(PeerConfig config, IChannelFactory channelFactory)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            Config = config;
            ChannelFactory = channelFactory;

            _socket = new SocketWrapper(Config);
            _socket.PacketReceived += OnPacketReceived;

            _state = PeerState.NotStarted;
        }


        private static ILog Log { get; } = LogProvider.For<Peer>();


        public PeerConfig Config { get; }

        public IChannelFactory ChannelFactory { get; }

        public bool IsRunning => _state == PeerState.Running;


        public void Start()
        {
            if (_state == PeerState.Starting || _state == PeerState.Running)
            {
                return;
            }
            else if (_state != PeerState.NotStarted)
            {
                throw new NetException("Peer is in wrong state.");
            }

            _state = PeerState.Starting;
            Log.Trace("Starting peer");

            try
            {
                OnStart();

                _state = PeerState.Running;
                Log.Trace("Peer started");
            }
            catch
            {
                _state = PeerState.Error;
                throw;
            }
        }

        public void Stop()
        {
            if (_state == PeerState.Stopping || _state == PeerState.Stopped)
            {
                return;
            }
            else if (_state != PeerState.Running)
            {
                throw new NetException("Peer is in wrong state.");
            }

            _state = PeerState.Stopping;
            Log.Trace("Stopping peer");

            try
            {
                OnStop();

                _state = PeerState.Stopped;
                Log.Debug("Peer stopped");

                var statistics = _socket.Statistics;
                Log.Debug("Received bytes: {ReceivedBytes}", statistics.ReceivedBytes);
                Log.Debug("Received packets: {ReceivedPackets}", statistics.ReceivedPackets);
                Log.Debug("Sent bytes: {SentBytes}", statistics.SentBytes);
                Log.Debug("Sent packets: {SentPackets}", statistics.SentPackets);
            }
            catch
            {
                _state = PeerState.Error;
                throw;
            }
        }

        public void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            OnUpdate();
        }


        protected virtual void OnStart()
        {
            _socket.Bind();
        }

        protected virtual void OnStop()
        {
            _socket.Unbind();
        }

        protected abstract void OnUpdate();


        internal abstract void OnConnect(Connection connection);

        internal abstract void OnDisconnect(Connection connection);

        internal abstract void OnPacketReceived(IPEndPoint remoteEndPoint, byte channelId, NetDataReader reader);

        internal void SendPacket(IPEndPoint remoteEndPoint, byte channelId, IPacket packet)
        {
            _socket.Send(remoteEndPoint, channelId, packet);
        }


        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();

                    _socket.PacketReceived -= OnPacketReceived;
                    _socket.Dispose();
                }
                _disposed = true;
            }
        }


        private enum PeerState
        {
            Error,
            NotStarted,
            Starting,
            Running,
            Stopping,
            Stopped,
        }
    }
}
