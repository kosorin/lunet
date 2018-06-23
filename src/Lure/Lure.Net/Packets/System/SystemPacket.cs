using Lure.Net.Data;

namespace Lure.Net.Packets.System
{
    internal abstract class SystemPacket : Packet
    {
        public PacketDataType DataType { get; set; }

        public PacketData Data { get; set; }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            DataType = (PacketDataType)reader.ReadByte();
        }

        protected sealed override void DeserializeDataCore(INetDataReader reader)
        {
            Data.Deserialize(reader);
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteByte((byte)DataType);
        }

        protected sealed override void SerializeDataCore(INetDataWriter writer)
        {
            Data.Serialize(writer);
        }
    }
}
