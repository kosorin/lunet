using System;

namespace Lure.Net.Channels.Message
{
    public class UnreliablePacket : MessagePacket<UnreliableMessage>
    {
        public UnreliablePacket(Func<UnreliableMessage> messageActivator) : base(messageActivator)
        {
        }
    }
}
