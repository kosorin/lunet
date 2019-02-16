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

            var packet = _packetActivator();
            var packetLength = packet.HeaderLength;
            foreach (var message in messages)
            {
                if (packetLength + message.Length > maxPacketSize)
                {
                    if (packet.Messages.Count == 0)
                    {
                        // TODO: Message too big -> fragment
                        throw new NotSupportedException("Message too big");
                    }

                    packets.Add(packet);

                    packet = _packetActivator();
                    packetLength = 0;
                }

                packet.Messages.Add(message);
                packetLength += message.Length;
            }
            if (packetLength > 0)
            {
                packets.Add(packet);
            }

            return packets;
        }
    }
}
