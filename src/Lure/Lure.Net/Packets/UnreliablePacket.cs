using System;

namespace Lure.Net.Packets
{
    internal class UnreliablePacket : NetPacket<RawMessage>
    {
        public UnreliablePacket(Func<RawMessage> rawMessageActivator) : base(rawMessageActivator)
        {
        }
    }
}
