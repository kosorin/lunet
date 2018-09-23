using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Net;

namespace Lure.Net
{
    public abstract class Peer : IDisposable
    {
        public static byte Version => 1;


        private readonly PeerConfig _config;
        private readonly SocketWrapper _socket;

        private volatile PeerState _state;

        private protected Peer(PeerConfig config)
        {
            if (!config.IsLocked)
            {
                config.Lock();
            }
            _config = config;

            _socket = new SocketWrapper(_config);
            _socket.PacketReceived += OnPacketReceived;

            _state = PeerState.NotStarted;
        }


        public PeerConfig Config => _config;

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
            Log.Verbose("Starting peer");

            try
            {
                OnStart();

                _state = PeerState.Running;
                Log.Debug("Peer started");
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
            Log.Verbose("Stopping peer");

            try
            {
                OnStop();

                _state = PeerState.Stopped;
                Log.Debug("Peer stopped");

                var statistics = _socket.Statistics;
                Log.Information("Received bytes: {ReceivedBytes}", statistics.ReceivedBytes);
                Log.Information("Received packets: {ReceivedPackets}", statistics.ReceivedPackets);
                Log.Information("Sent bytes: {SentBytes}", statistics.SentBytes);
                Log.Information("Sent packets: {SentPackets}", statistics.SentPackets);
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

        internal void SendPacket(IPEndPoint remoteEndPoint, byte channelId, INetPacket packet)
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
