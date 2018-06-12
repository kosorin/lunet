using Lure.Net.Data;
using Lure.Net.Messages;
using System.Net.Sockets;

namespace Lure.Net.Extensions
{
    internal static class NetSerializableExtensions
    {
        public static void ReadSerializable(this INetDataReader reader, INetSerializable serializable)
        {
            serializable.Deserialize(reader);
        }

        public static void WriteSerializable(this INetDataWriter writer, INetSerializable serializable)
        {
            serializable.Serialize(writer);
        }

        public static SequenceNumber ReadSequenceNumber(this INetDataReader reader)
        {
            return new SequenceNumber(reader.ReadUShort());
        }

        public static void WriteSequenceNumber(this INetDataWriter writer, SequenceNumber sequence)
        {
            writer.WriteUShort(sequence.Value);
        }
    }
}
