using Lunet.Common;
using System;
using System.Collections.Generic;

namespace Lunet.Channels
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


        protected IList<TPacket>? PackOutgoingPackets()
        {
            var outgoingMessages = CollectOutgoingMessages();
            var outgoingPackets = MessagePacker.Pack(outgoingMessages, Connection.MTU);

            return outgoingPackets;
        }

        protected abstract IList<TMessage>? CollectOutgoingMessages();
    }
}
