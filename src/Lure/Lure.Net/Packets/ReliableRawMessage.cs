using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class ReliableRawMessage : IRawMessage
    {
        public long Timestamp { get; set; }

        public SeqNo Seq { get; set; }

        public byte[] Data { get; set; }

        public int Length => (2 * sizeof(ushort)) + Data.Length;

        public void Deserialize(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            Data = reader.ReadByteArray();
        }

        public void Serialize(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            writer.WriteByteArray(Data);
        }
    }
}
