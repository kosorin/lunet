using Lure.Collections;
using Lure.Net.Data;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    internal abstract class PayloadPacketData<TRawMessage> : PacketData, IPoolable
        where TRawMessage : IRawMessage, new()
    {
        public List<TRawMessage> RawMessages { get; } = new List<TRawMessage>();

        public override int Length => RawMessages.Sum(x => x.Length);

        public override void Deserialize(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                var rawMessage = new TRawMessage();
                rawMessage.Deserialize(reader);
                RawMessages.Add(rawMessage);
            }
        }

        public override void Serialize(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                rawMessage.Serialize(writer);
            }
        }

        public void OnRent()
        {
        }

        public void OnReturn()
        {
            RawMessages.Clear();
        }
    }
}
