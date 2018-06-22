using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    [PacketData(PacketDataType.PayloadReliable)]
    [PacketData(PacketDataType.PayloadReliableSequenced)]
    [PacketData(PacketDataType.PayloadReliableOrdered)]
    internal class ReliablePayloadPacketData : PacketData, IPoolable
    {
        public Dictionary<SeqNo, byte[]> RawMessages { get; } = new Dictionary<SeqNo, byte[]>();

        public override int Length => RawMessages.Sum(x => (2 * sizeof(ushort)) + x.Value.Length);

        public override void Deserialize(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                RawMessages.Add(reader.ReadSeqNo(), reader.ReadByteArray());
            }
        }

        public override void Serialize(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                writer.WriteSeqNo(rawMessage.Key);
                writer.WriteByteArray(rawMessage.Value);
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
