using Lure.Net.Data;
using Lure.Net.Messages;
using System.Net.Sockets;

namespace Lure.Net.Extensions
{
    internal static class NetSerializableExtensions
    {
        public static void SetTokenBuffer(this NetDataWriter writer, SocketAsyncEventArgs token)
        {
            token.SetBuffer(writer.Data, writer.Offset, writer.Length);
        }

        public static void ReadSerializable(this INetDataReader reader, INetSerializable serializable)
        {
            serializable.Deserialize(reader);
        }

        public static void WriteSerializable(this INetDataWriter writer, INetSerializable serializable)
        {
            serializable.Serialize(writer);
        }
    }
}
