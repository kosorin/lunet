using System;

namespace Bur.Net.Server
{
    public class NetClientConnectedEventArgs : EventArgs
    {
        public NetClientConnectedEventArgs(INetClient client)
        {
            Client = client;
        }

        public INetClient Client { get; }
    }
}
