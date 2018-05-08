using System;

namespace Bur.Net.Server
{
    public class NetClientDisconnectedEventArgs : EventArgs
    {
        public NetClientDisconnectedEventArgs(INetClient client)
        {
            Client = client;
        }

        public INetClient Client { get; }
    }
}
