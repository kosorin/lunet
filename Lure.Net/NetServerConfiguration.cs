namespace Lure.Net
{
    public class NetServerConfiguration : NetPeerConfiguration
    {
        public new int LocalPort
        {
            get => base.LocalPort ?? throw new ConfigurationException("Local port is not set.");
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
