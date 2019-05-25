using Lunet.Data;

namespace Lunet.Extensions
{
    public static class SerializationExtensions
    {
        public static SeqNo ReadSeqNo(this NetDataReader reader)
        {
            return new SeqNo(reader.ReadUShort());
        }

        public static void WriteSeqNo(this NetDataWriter writer, SeqNo seq)
        {
            writer.WriteUShort(seq.Value);
        }
    }
}
