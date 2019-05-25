using System.Collections.Generic;

namespace Lunet.Channels
{
    public interface IMessagePacker<TPacket, TMessage>
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        IList<TPacket>? Pack(IList<TMessage>? messages, int maxPacketSize);
    }
}
