namespace Lunet.Channels;

public interface IMessagePacker<TPacket, TMessage>
    where TPacket : MessagePacket<TMessage>
    where TMessage : Message
{
    List<TPacket>? Pack(List<TMessage>? messages, int maxPacketSize);
}
