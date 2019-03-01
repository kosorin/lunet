using System;
using System.Collections.Generic;

namespace Lunet.Channels
{
    public abstract class MessagePacker<TPacket, TMessage> : IMessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        private readonly Func<TPacket> _packetActivator;

        public MessagePacker(Func<TPacket> packetActivator)
        {
            _packetActivator = packetActivator;
        }

        public abstract IList<TPacket> Pack(IList<TMessage> messages, int maxPacketSize);

        protected TPacket CreatePacket()
        {
            return _packetActivator();
        }
    }
}
