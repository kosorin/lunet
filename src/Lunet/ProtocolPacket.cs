using Force.Crc32;
using System;

namespace Lunet
{
    public class ProtocolPacket
    {
        private static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        private static uint Crc32Check { get; } = 0x2144DF1C;

        private static int Crc32Length { get; } = sizeof(uint);

        private static uint InitialCrc32 { get; }

        static ProtocolPacket()
        {
            InitialCrc32 = Crc32Algorithm.Compute(Version.ToByteArray());
        }


        public byte ChannelId { get; set; }

        public IChannelPacket ChannelPacket { get; set; }


        //public bool Deserialize(NetDataReader reader)
        //{
        //    var crc32 = Crc32Algorithm.Append(InitialCrc32, reader.Data, reader.Offset, reader.Length);
        //    if (crc32 != Crc32Check)
        //    {
        //        return false;
        //    }

        //    ChannelId = reader.ReadByte();
        //    //var reader = new NetDataReader(data, offset, length - Crc32Length);

        //    return false;//            return (reader.ReadByte(), reader);
        //}

        //public void Serialize(NetDataWriter writer)
        //{

        //}
    }
}
