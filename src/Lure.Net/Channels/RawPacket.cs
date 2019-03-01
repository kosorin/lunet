using Lunet.Data;

namespace Lunet.Channels
{
    public class RawPacket : ChannelPacket
    {
        public byte[] Data { get; set; }

        public override int HeaderLength => 0;

        public override int DataLength => Data.Length;

        public override void DeserializeHeader(NetDataReader reader)
        {
        }

        public override void DeserializeData(NetDataReader reader)
        {
            Data = reader.ReadBytesToEnd();
        }

        public override void SerializeHeader(NetDataWriter writer)
        {
        }

        public override void SerializeData(NetDataWriter writer)
        {
            writer.WriteBytes(Data);
        }
    }
}
