using Lure.Collections;
using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal abstract class Packet
    {
        public byte ChannelId { get; set; }

        public PacketDataType DataType { get; set; }

        public PacketData Data { get; set; }

        private static int SerializeCheck => 0x55555555;

        public void DeserializeHeader(INetDataReader reader)
        {
            // Skip reading a channel id - already read and used to create a channel

            DeserializeHeaderCore(reader);

            reader.PadBits();
            if (reader.ReadInt() != SerializeCheck)
            {
                // TODO: Handle bad packets
                throw new NetException();
            }

            DataType = (PacketDataType)reader.ReadByte();
        }

        public void DeserializeData(INetDataReader reader)
        {
            Data.Deserialize(reader);

            if (reader.Position != reader.Length)
            {
                // TODO: Handle bad packets
                throw new NetException("Remaining data in a packet.");
            }
        }

        public void SerializeHeader(INetDataWriter writer)
        {
            writer.WriteByte(ChannelId);

            SerializeHeaderCore(writer);

            writer.PadBits();
            writer.WriteInt(SerializeCheck);

            writer.WriteByte((byte)DataType);
        }

        public void SerializeData(INetDataWriter writer)
        {
            Data.Serialize(writer);
        }

        protected abstract void DeserializeHeaderCore(INetDataReader reader);

        protected abstract void SerializeHeaderCore(INetDataWriter writer);
    }
}
