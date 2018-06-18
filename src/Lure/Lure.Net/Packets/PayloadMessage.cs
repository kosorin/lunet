using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;

namespace Lure.Net.Packets
{
    internal class PayloadMessage : IPacketPart
    {
        public SeqNo Seq { get; set; }

        public byte[] Data { get; set; }


        public long? LastSendTimestamp { get; set; }

        public int Length => (2 * sizeof(ushort)) + Data.Length;

        public void Deserialize(INetDataReader reader)
        {
            reader.PadBits();
            Seq = reader.ReadSeqNo();
            Data = reader.ReadByteArray();
        }

        public void Serialize(INetDataWriter writer)
        {
            writer.PadBits();
            writer.WriteSeqNo(Seq);
            writer.WriteByteArray(Data);
        }
    }
}
