using Lunet.Data;
using System;

namespace Lunet.Channels
{
    public class RawPacket : ChannelPacket
    {
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public override int DataLength => Data.Length;

        public override void DeserializeHeader(NetDataReader reader)
        {
        }

        public override void DeserializeData(NetDataReader reader)
        {
            Data = reader.ReadBytes();
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
