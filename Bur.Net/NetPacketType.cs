using System;
using System.Collections.Generic;
using System.Text;

namespace Bur.Net
{

    public enum NetPacketType
    {
        Quick,
        Ping,
        Pong,
        ConnectRequest,
        ConnectAccept,
        Disconnect,
    }
}
