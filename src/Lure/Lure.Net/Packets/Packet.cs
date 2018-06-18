using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal abstract class Packet : INetSerializable
    {
        public static int SerializeCheck => 0x55555555;

        public byte ChannelId { get; set; }

        public PacketDataType Type { get; set; }

        public PacketData Data { get; set; }


        void INetSerializable.Deserialize(INetDataReader reader)
        {
            // Skip reading channel id and type - already read and used to create a packet

            DeserializeCore(reader);

            reader.PadBits();
            if (reader.ReadInt() != SerializeCheck)
            {
                // TODO: Handle bad packets
                throw new NetException();
            }

            reader.ReadSerializable(Data);

            if (reader.Position != reader.Length)
            {
                // TODO: Handle bad packets
                throw new NetException("Remaining data in a packet.");
            }
        }

        void INetSerializable.Serialize(INetDataWriter writer)
        {
            writer.WriteByte(ChannelId);
            writer.WriteByte((byte)Type);

            SerializeCore(writer);

            writer.PadBits();
            writer.WriteInt(SerializeCheck);

            writer.WriteSerializable(Data);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
