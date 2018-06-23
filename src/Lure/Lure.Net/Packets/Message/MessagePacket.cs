using Lure.Collections;
using Lure.Net.Data;
using System.Collections.Generic;

namespace Lure.Net.Packets.Message
{
    internal abstract class MessagePacket<TRawMessage> : Packet, IPoolable
        where TRawMessage : RawMessage
    {
        protected readonly ObjectPool<TRawMessage> _rawMessagePool;

        protected MessagePacket(ObjectPool<TRawMessage> rawMessagePool)
        {
            _rawMessagePool = rawMessagePool;
        }

        public List<TRawMessage> RawMessages { get; } = new List<TRawMessage>();

        void IPoolable.OnRent()
        {
            RawMessages.Clear();
        }

        void IPoolable.OnReturn()
        {
            foreach (var rawMessage in RawMessages)
            {
                _rawMessagePool.Return(rawMessage);
            }
        }

        protected sealed override void DeserializeDataCore(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                var rawMessage = _rawMessagePool.Rent();
                rawMessage.Deserialize(reader);
                RawMessages.Add(rawMessage);
            }
        }

        protected sealed override void SerializeDataCore(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                rawMessage.Serialize(writer);
            }
        }
    }
}
