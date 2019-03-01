using System;
using System.Collections.Generic;

namespace Lunet.Channels
{
    public class DefaultMessagePacker<TPacket, TMessage> : MessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        public DefaultMessagePacker(Func<TPacket> packetActivator) : base(packetActivator)
        {
        }

        public override IList<TPacket> Pack(IList<TMessage> messages, int maxPacketSize)
        {
            if (messages == null || messages.Count == 0)
            {
                return null;
            }

            var packets = new List<TPacket>();

            var currentPacket = CreatePacket();
            var currentLength = currentPacket.HeaderLength;

            if (currentPacket.HeaderLength >= maxPacketSize)
            {
                throw new NetException("Too big packet header.");
            }

            foreach (var message in messages)
            {
                if (currentLength + message.Length > maxPacketSize && currentPacket.Messages.Count > 0)
                {
                    packets.Add(currentPacket);

                    currentPacket = CreatePacket();
                    currentLength = currentPacket.HeaderLength;

                    // Next packet may be too big even with single message => fragmentation
                }

                currentPacket.Messages.Add(message);
                currentLength += message.Length;
            }

            if (currentPacket.Messages.Count > 0)
            {
                packets.Add(currentPacket);
            }

            return packets;
        }
    }
}
