using Force.Crc32;
using Lunet.Data;
using System;

namespace Lunet
{
    public class ProtocolProcessor
    {
        private static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        private static uint Crc32Check { get; } = 0x2144DF1C;

        private static int Crc32Length { get; } = sizeof(uint);

        private static uint InitialCrc32 { get; }

        static ProtocolProcessor()
        {
            InitialCrc32 = Crc32Algorithm.Compute(Version.ToByteArray());
        }

        public (byte ChannelId, NetDataReader? Reader) Read(byte[] data, int offset, int length)
        {
            var crc32 = Crc32Algorithm.Append(InitialCrc32, data, offset, length);
            if (crc32 != Crc32Check)
            {
                return (default, null);
            }

            var reader = new NetDataReader(data, offset, length - Crc32Length);

            return (reader.ReadByte(), reader);
        }

        public void Write(NetDataWriter writer, byte channelId, IChannelPacket packet)
        {
            var offset = writer.Length;

            // Packet
            writer.WriteByte(channelId);
            packet.SerializeHeader(writer);
            packet.SerializeData(writer);
            writer.Flush();

            var length = writer.Length - offset;

            // CRC
            var crc32 = Crc32Algorithm.Append(InitialCrc32, writer.Data, offset, length);
            writer.WriteUInt(crc32);
            writer.Flush();
        }

        public ushort GetTotalLength(IChannelPacket packet)
        {
            return (ushort)(1 + Crc32Length + packet.Length);
        }
    }
}
