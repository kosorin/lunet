using System;
using System.Collections.Generic;
using System.Text;

namespace Bur.Net
{
    public abstract class NetMessage
    {
        public NetMessageType Type { get; set; }
    }

    public enum NetMessageType
    {
        Quick,
        Ping,
        Pong,
        ConnectRequest,
        ConnectAccept,
        Disconnect,
    }
}
