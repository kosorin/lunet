using System.Net;

namespace Lure.Net
{
    public class ClientPeerConfig : PeerConfig
    {
        private string _hostname = "localhost";
        private int _port;


        public ClientPeerConfig()
        {
            MaximumConnections = 1;
        }


        public string Hostname
        {
            get => _hostname;
            set => Set(ref _hostname, value);
        }

        public int Port
        {
            get => _port;
            set => Set(ref _port, value);
        }


        protected override void OnLock()
        {
            if (string.IsNullOrWhiteSpace(Hostname))
            {
                throw new ConfigurationException("Hostname is not set.");
            }

            if (Port < IPEndPoint.MinPort || Port > IPEndPoint.MaxPort)
            {
                throw new ConfigurationException($"Port {Port} is out of range.");
            }

            base.OnLock();
        }
    }
}
