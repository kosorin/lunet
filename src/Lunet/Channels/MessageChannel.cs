namespace Lunet.Channels;

public abstract class MessageChannel<TPacket, TMessage> : Channel<TPacket>
    where TPacket : MessagePacket<TMessage>
    where TMessage : Message
{
    protected MessageChannel(byte id, Connection connection) : base(id, connection)
    {
    }

    protected abstract Func<TMessage> MessageActivator { get; }

    protected abstract IMessagePacker<TPacket, TMessage> MessagePacker { get; }

    protected List<TPacket>? PackOutgoingPackets()
    {
        var outgoingMessages = CollectOutgoingMessages();
        var outgoingPackets = MessagePacker.Pack(outgoingMessages, Connection.MTU);

        return outgoingPackets;
    }

    protected abstract List<TMessage>? CollectOutgoingMessages();
}
