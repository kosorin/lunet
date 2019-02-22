using System;
using System.Net;

namespace Lure.Net
{
    public class ServerConfiguration : PeerConfiguration
    {
        public new int LocalPort
        {
            get => base.LocalPort ?? IPEndPoint.MinPort;
            set => base.LocalPort = value;
        }


        protected override void OnLock()
        {
            if (!base.LocalPort.HasValue)
            {
                throw new Exception("Local port is not set.");
            }

            base.OnLock();
        }
    }
}
