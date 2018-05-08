using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Pur.Server
{
    public class ConnectedEventArgs : EventArgs
    {
        public ConnectedEventArgs(TcpClient client)
        {
            Client = client;
        }

        public TcpClient Client { get; }
    }
}
