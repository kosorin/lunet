using Lure.Net.Data;

namespace Lure.Net.Extensions
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

        public static byte[] ReadByteArray(this NetDataReader reader)
        {
            var length = reader.ReadUShort();
            var array = reader.ReadBytes(length);
            return array;
        }

        public static void WriteByteArray(this NetDataWriter writer, byte[] array)
        {
            writer.WriteUShort((ushort)array.Length);
            writer.WriteBytes(array);
        }
    }
}
