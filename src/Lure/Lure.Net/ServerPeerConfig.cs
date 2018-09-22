﻿using System.Net;

namespace Lure.Net
{
    public class ServerPeerConfig : PeerConfig
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
                throw new ConfigurationException("Local port is not set.");
            }

            base.OnLock();
        }
    }
}