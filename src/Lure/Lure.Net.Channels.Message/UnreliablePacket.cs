using System;

namespace Lure.Net.Channels.Message
{
    public class UnreliablePacket : MessagePacket<Message>
    {
        public UnreliablePacket(Func<Message> messageActivator) : base(messageActivator)
        {
        }
    }
}
