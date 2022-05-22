namespace Lunet.Channels;

public class UnreliablePacket : MessagePacket<UnreliableMessage>
{
    public UnreliablePacket(Func<UnreliableMessage> messageActivator) : base(messageActivator)
    {
    }
}
