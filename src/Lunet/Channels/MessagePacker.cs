namespace Lunet.Channels;

public abstract class MessagePacker<TPacket, TMessage> : IMessagePacker<TPacket, TMessage>
    where TPacket : MessagePacket<TMessage>
    where TMessage : Message
{
    private readonly Func<TPacket> _packetActivator;

    protected MessagePacker(Func<TPacket> packetActivator)
    {
        _packetActivator = packetActivator ?? throw new ArgumentNullException(nameof(packetActivator));
    }

    public abstract List<TPacket>? Pack(List<TMessage>? messages, int maxPacketSize);

    protected TPacket CreatePacket()
    {
        return _packetActivator();
    }
}
