using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    [PacketData(PacketDataType.PayloadUnreliable)]
    [PacketData(PacketDataType.PayloadUnreliableSequenced)]
    internal class UnreliablePayloadPacketData : PacketData, IPoolable
    {
        public List<byte[]> RawMessages { get; } = new List<byte[]>();

        public override int Length => RawMessages.Sum(x => sizeof(ushort) + x.Length);

        public override void Deserialize(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                RawMessages.Add(reader.ReadByteArray());
            }
        }

        public override void Serialize(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                writer.WriteByteArray(rawMessage);
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
