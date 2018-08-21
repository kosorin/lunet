using System.Net;

namespace Lure.Net
{
    public class NetServerConfiguration : NetPeerConfiguration
    {
        public NetServerConfiguration()
        {
            AcceptIncomingConnections = true;
        }


        public new int LocalPort
        {
            get => base.LocalPort ?? IPEndPoint.MinPort;
            set => base.LocalPort = value;
        }


        public override void Validate()
        {
            if (!base.LocalPort.HasValue)
            {
                throw new ConfigurationException("Local port is not set.");
            }

            base.Validate();
        }
    }
}
