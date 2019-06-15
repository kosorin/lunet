using Lunet.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunet.Channels
{
    public abstract class MessagePacket<TMessage> : ChannelPacket
        where TMessage : Message
    {
        private readonly Func<TMessage> _messageActivator;

        protected MessagePacket(Func<TMessage> messageActivator)
        {
            _messageActivator = messageActivator;
        }

        public List<TMessage> Messages { get; } = new List<TMessage>();

        public override int HeaderLength => 0;

        public override int DataLength => Messages.Sum(x => x.Length);

        public override void DeserializeHeader(NetDataReader reader)
        {
            try
            {
                DeserializeHeaderCore(reader);
                reader.PadByte();
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not deserialize packet header.", e);
            }
        }

        public override void DeserializeData(NetDataReader reader)
        {
            try
            {
                DeserializeDataCore(reader);

                if (reader.Position != reader.Length)
                {
                    throw new NetSerializationException($"Remaining data in a packet ({reader.Length - reader.Position} bytes).");
                }
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not deserialize packet data.", e);
            }
        }

        public override void SerializeHeader(NetDataWriter writer)
        {
            try
            {
                SerializeHeaderCore(writer);
                writer.PadByte();
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not serialize packet header.", e);
            }
        }

        public override void SerializeData(NetDataWriter writer)
        {
            try
            {
                SerializeDataCore(writer);
                writer.PadByte();
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not serialize packet data.", e);
            }
        }

        protected virtual void DeserializeHeaderCore(NetDataReader reader)
        {
        }

        protected virtual void DeserializeDataCore(NetDataReader reader)
        {
            Messages.Clear();
            while (reader.Position < reader.Length)
            {
                var message = _messageActivator();
                message.Deserialize(reader);
                Messages.Add(message);
            }
        }

        protected virtual void SerializeHeaderCore(NetDataWriter writer)
        {
        }

        protected virtual void SerializeDataCore(NetDataWriter writer)
        {
            foreach (var message in Messages)
            {
                message.Serialize(writer);
            }
        }
    }
}
