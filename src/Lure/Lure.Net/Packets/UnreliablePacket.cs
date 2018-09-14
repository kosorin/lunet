using System;

namespace Lure.Net.Packets
{
    public class UnreliablePacket : NetPacket<RawMessage>
    {
        public UnreliablePacket(Func<RawMessage> rawMessageActivator) : base(rawMessageActivator)
        {
        }
    }
}
