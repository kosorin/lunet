using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    internal class Payload : IPacketPart
    {
        public List<PayloadMessage> Messages { get; } = new List<PayloadMessage>();

        public int Length => Messages.Sum(x => x.Length);
    }
}
