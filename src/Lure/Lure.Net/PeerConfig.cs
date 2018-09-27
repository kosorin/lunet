using Lure.Net.Channels;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public abstract class PeerConfig : Configuration, ISocketConfig
    {
        private int? _localPort = null;
        private AddressFamily _addressFamily = AddressFamily.InterNetwork;
        private bool _dualMode = false;
        private int _receiveBufferSize = 10 * 1024 * 1024; // 10 MB
        private int _sendBufferSize = 10 * 1024 * 1024; // 10 MB
        private int _packetBufferSize = 2 * 1024; // 2 kB
        private int _messageBufferSize = 32; // 32 B
        private int _connectionTimeout = 6_000; // 6 seconds
        private int _maximumConnections = 32;
        private INetChannelFactory _channelFactory = new NetChannelFactory();


        public int? LocalPort
        {
            get => _localPort;
            set => Set(ref _localPort, value);
        }

        public AddressFamily AddressFamily
        {
            get => _addressFamily;
            set => Set(ref _addressFamily, value);
        }

        public bool DualMode
        {
            get => _dualMode;
            set => Set(ref _dualMode, value);
        }

        public int ReceiveBufferSize
        {
            get => _receiveBufferSize;
            set => Set(ref _receiveBufferSize, value);
        }

        public int SendBufferSize
        {
            get => _sendBufferSize;
            set => Set(ref _sendBufferSize, value);
        }

        public int PacketBufferSize
        {
            get => _packetBufferSize;
            set => Set(ref _packetBufferSize, value);
        }

        public int MessageBufferSize
        {
            get => _messageBufferSize;
            set => Set(ref _messageBufferSize, value);
        }

        public int ConnectionTimeout
        {
            get => _connectionTimeout;
            set => Set(ref _connectionTimeout, value);
        }

        public int MaximumConnections
        {
            get => _maximumConnections;
            set => Set(ref _maximumConnections, value);
        }

        public INetChannelFactory ChannelFactory
        {
            get => _channelFactory;
            set => Set(ref _channelFactory, value);
        }


        protected override void OnLock()
        {
            if (LocalPort.HasValue && (LocalPort < IPEndPoint.MinPort || LocalPort > IPEndPoint.MaxPort))
            {
                throw new ConfigurationException($"Local port {LocalPort} is out of range.");
            }

            if (AddressFamily != AddressFamily.InterNetwork && AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ConfigurationException("Configuration accepta only IPv4 or IPv6 addresses.");
            }

            if (DualMode && AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ConfigurationException("Dual mode is available only for IPv6 addresses.");
            }

            if (AddressFamily == AddressFamily.InterNetwork && !Socket.OSSupportsIPv4)
            {
                throw new ConfigurationException("IPv4 not supported.");
            }

            if (AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6)
            {
                throw new ConfigurationException("IPv6 not supported.");
            }
        }
    }
}
