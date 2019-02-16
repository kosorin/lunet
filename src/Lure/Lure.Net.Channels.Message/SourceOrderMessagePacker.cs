using System;
using System.Collections.Generic;

namespace Lure.Net.Channels.Message
{
    internal class SourceOrderMessagePacker<TPacket, TMessage> : IMessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        private readonly Func<TPacket> _packetActivator;

        public SourceOrderMessagePacker(Func<TPacket> packetActivator)
        {
            _packetActivator = packetActivator;
        }

        public IList<TPacket> Pack(IList<TMessage> messages, int maxPacketSize)
        {
            if (messages == null || messages.Count == 0)
            {
                return null;
            }

            var packets = new List<TPacket>();

            var currentPacket = _packetActivator();
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

                    currentPacket = _packetActivator();
                    currentLength = currentPacket.HeaderLength;
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
