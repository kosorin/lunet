using System;

namespace Lure.Net.Channels.Message
{
    public abstract class MessageChannel<TPacket, TMessage> : Channel
        where TPacket : MessagePacket<TMessage>
        where TMessage : Message
    {
        protected MessageChannel(byte id, Connection connection) : base(id, connection)
        {
            MessageActivator = ObjectActivatorFactory.Create<TMessage>();
            PacketActivator = ObjectActivatorFactory.CreateWithValues<Func<TMessage>, TPacket>(MessageActivator);
            MessagePacker = new DefaultMessagePacker<TPacket, TMessage>(PacketActivator);
        }


        protected Func<TPacket> PacketActivator { get; }

        protected Func<TMessage> MessageActivator { get; }

        protected IMessagePacker<TPacket, TMessage> MessagePacker { get; }
    }
}
