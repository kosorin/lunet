using Lure.Collections;
using Lure.Net.Data;
using System.Collections.Generic;

namespace Lure.Net.Packets
{
    internal abstract class MessagePacket<TRawMessage> : Packet
        where TRawMessage : RawMessageBase
    {
        protected readonly ObjectPool<TRawMessage> _rawMessagePool;

        protected MessagePacket(ObjectPool<TRawMessage> rawMessagePool)
        {
            _rawMessagePool = rawMessagePool;
        }

        public List<TRawMessage> RawMessages { get; set; } = new List<TRawMessage>();

        protected override void DeserializeDataCore(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                var rawMessage = _rawMessagePool.Rent();
                rawMessage.Deserialize(reader);
                RawMessages.Add(rawMessage);
            }
        }

        protected override void SerializeDataCore(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                rawMessage.Serialize(writer);
            }
        }
    }
}
