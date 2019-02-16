using System.Collections.Generic;

namespace Lure.Net.Channels.Message
{
    public interface IMessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        IList<TPacket> Pack(IList<TMessage> messages, int maxPacketSize);
    }
}
