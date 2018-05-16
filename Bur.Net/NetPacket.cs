using System;
using System.Collections.Generic;
using System.Text;

namespace Bur.Net
{
    public abstract class NetPacket
    {
        public NetPacketType Type { get; set; }
    }
}
