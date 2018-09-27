using System;
using System.Collections.Generic;

namespace Lure.Net.Channels.Message
{
    internal interface IMessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        List<TPacket> Pack(List<TMessage> messages, int maxPacketSize);
    }
}
