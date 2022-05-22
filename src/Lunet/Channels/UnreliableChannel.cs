using Lunet.Common;
using Lunet.Data;

namespace Lunet.Channels;

public class UnreliableChannel : MessageChannel<UnreliablePacket, UnreliableMessage>
{
    private readonly List<UnreliableMessage> _outgoingMessageQueue = new List<UnreliableMessage>();
    private readonly List<UnreliableMessage> _incomingMessageQueue = new List<UnreliableMessage>();

    public UnreliableChannel(byte id, Connection connection) : base(id, connection)
    {
        MessageActivator = ObjectActivatorFactory.Create<UnreliableMessage>();
        PacketActivator = ObjectActivatorFactory.CreateWithValues<Func<UnreliableMessage>, UnreliablePacket>(MessageActivator);
        MessagePacker = new UnreliableMessagePacker<UnreliablePacket, UnreliableMessage>(PacketActivator);
    }

    protected override Func<UnreliablePacket> PacketActivator { get; }

    protected override Func<UnreliableMessage> MessageActivator { get; }

    protected override IMessagePacker<UnreliablePacket, UnreliableMessage> MessagePacker { get; }


    public override List<byte[]>? GetReceivedMessages()
    {
        lock (_incomingMessageQueue)
        {
            if (_incomingMessageQueue.Count == 0)
            {
                return null;
            }

            var receivedMessages = _incomingMessageQueue.Select(x => x.Data).ToList();
            _incomingMessageQueue.Clear();
            return receivedMessages;
        }
    }

    public override void SendMessage(byte[] data)
    {
        lock (_outgoingMessageQueue)
        {
            var message = MessageActivator();
            message.Data = data;
            _outgoingMessageQueue.Add(message);
        }
    }


    internal override void HandleIncomingPacket(NetDataReader reader)
    {
        var packet = PacketActivator();

        try
        {
            packet.DeserializeHeader(reader);
            packet.DeserializeData(reader);
        }
        catch (NetSerializationException)
        {
            return;
        }

        if (packet.Messages.Count == 0)
        {
            return;
        }

        lock (_incomingMessageQueue)
        {
            foreach (var message in packet.Messages)
            {
                _incomingMessageQueue.Add(message);
            }
        }
    }

    internal override List<ChannelPacket>? CollectOutgoingPackets()
    {
        return PackOutgoingPackets()?.Cast<ChannelPacket>().ToList();
    }


    protected override List<UnreliableMessage>? CollectOutgoingMessages()
    {
        lock (_outgoingMessageQueue)
        {
            if (_outgoingMessageQueue.Count == 0)
            {
                return null;
            }

            var messages = _outgoingMessageQueue.ToList();
            _outgoingMessageQueue.Clear();
            return messages;
        }
    }
}
