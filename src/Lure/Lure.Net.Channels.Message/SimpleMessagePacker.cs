﻿using System;
using System.Collections.Generic;

namespace Lure.Net.Channels.Message
{
    internal class SimpleMessagePacker<TPacket, TMessage> : IMessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        private readonly Func<TPacket> _packetActivator;

        public SimpleMessagePacker(Func<TPacket> packetActivator)
        {
            _packetActivator = packetActivator;
        }

        public List<TPacket> Pack(List<TMessage> messages, int maxPacketSize)
        {
            if (messages.Count == 0)
            {
                return null;
            }

            var packets = new List<TPacket>();

            var packet = _packetActivator();
            var packetLength = 0; // TODO: Include packet header length
            foreach (var message in messages)
            {
                if (packetLength + message.Length > maxPacketSize)
                {
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