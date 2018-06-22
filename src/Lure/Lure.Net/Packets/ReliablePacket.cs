using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class ReliablePacket : SequencedPacket
    {
        public static int AckBufferLength { get; } = 64;

        public static int PacketAckBufferLength { get; } = sizeof(uint) * NC.BitsPerByte;

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            base.DeserializeHeaderCore(reader);

            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            base.SerializeHeaderCore(writer);

            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
