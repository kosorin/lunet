using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lunet
{
    internal abstract class PacketProcessor
    {
        public abstract PacketType Type { get; }

        public abstract int HeaderLength { get; }
    }

    internal class ChannelPacketProcessor : PacketProcessor
    {
        public override PacketType Type => PacketType.Channel;

        public override int HeaderLength => sizeof(byte);
    }
}
