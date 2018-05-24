using System.Net;

namespace Lure.Net
{
    public class NetClientConfiguration : NetPeerConfiguration
    {
        private string _hostname;
        private int _port;


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


        public override void Validate()
        {
            if (string.IsNullOrWhiteSpace(Hostname))
            {
                throw new ConfigurationException("Hostname is not set.");
            }

            if (Port < IPEndPoint.MinPort || Port > IPEndPoint.MaxPort)
            {
                throw new ConfigurationException($"Port {Port} is out of range.");
            }

            base.Validate();
        }
    }
}
