using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class ReliablePacket : SequencedPacket
    {
        public static int AckBufferLength => 64;

        public static int PacketAckBufferLength => sizeof(uint) * NC.BitsPerByte;

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        protected override void DeserializeCore(INetDataReader reader)
        {
            base.DeserializeCore(reader);

            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            base.SerializeCore(writer);

            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
