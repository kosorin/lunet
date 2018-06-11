using System.Collections.Generic;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;

namespace Lure.Net.Packets
{
    [Packet(PacketType.Payload)]
    internal class PayloadPacket : Packet
    {
        public override PacketType Type => PacketType.Payload;

        public byte[] Data { get; set; }

        protected override void DeserializeCore(INetDataReader reader)
        {
            Data = reader.ReadBytesToEnd();
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            writer.WriteBytes(Data);
        }
    }
}
