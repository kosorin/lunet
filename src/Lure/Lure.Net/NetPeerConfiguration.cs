using Lure.Net.Channels;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    public abstract class NetPeerConfiguration : Configuration
    {
        private bool _acceptIncomingConnections;
        private int? _localPort;
        private AddressFamily _addressFamily = AddressFamily.InterNetwork;
        private bool _dualMode;
        private int _sendBufferSize = 10 * 1024 * 1024; // 10 MB
        private int _receiveBufferSize = 10 * 1024 * 1024; // 10 MB
        private int _packetBufferSize = 2 * 1024; // 2 kB
        private int _closeTimeout = 2; // 2 seconds
        private int _maxClients = 32;
        private Dictionary<byte, NetChannelType> _channels = new Dictionary<byte, NetChannelType>
        {
            [NetConnection.DefaultChannelId] = NetChannelType.ReliableOrdered,
        };


        public bool AcceptIncomingConnections
        {
            get => _acceptIncomingConnections;
            set => Set(ref _acceptIncomingConnections, value);
        }

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

        public int SendBufferSize
        {
            get => _sendBufferSize;
            set => Set(ref _sendBufferSize, value);
        }

        public int ReceiveBufferSize
        {
            get => _receiveBufferSize;
            set => Set(ref _receiveBufferSize, value);
        }

        public int PacketBufferSize
        {
            get => _packetBufferSize;
            set => Set(ref _packetBufferSize, value);
        }

        public int CloseTimeout
        {
            get => _closeTimeout;
            set => Set(ref _closeTimeout, value);
        }

        public int MaxClients
        {
            get => _maxClients;
            set => Set(ref _maxClients, value);
        }

        public Dictionary<byte, NetChannelType> Channels
        {
            get => _channels;
            set => Set(ref _channels, value);
        }


        public override void Validate()
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
        }
    }
}
