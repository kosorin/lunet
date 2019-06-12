using Lunet.Common;
using Lunet.Data;
using System;

namespace Lunet
{
    public class ProtocolProcessor
    {
        private static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        private static uint InitialHash { get; }

        static ProtocolProcessor()
        {
            InitialHash = Crc32.Compute(Version.ToByteArray());
        }

        public (byte ChannelId, NetDataReader? Reader) Read(NetDataReader reader)
        {
            if (!Crc32.Check(InitialHash, reader.GetSpan()))
            {
                return (default, null);
            }

            var channelId = reader.ReadByte();
            reader.ResetRelative(sizeof(byte), -Crc32.HashLength);

            return (channelId, reader);
        }

        public void Write(NetDataWriter writer, byte channelId, IChannelPacket packet)
        {
            var offset = writer.Position;

            // Packet
            writer.WriteByte(channelId);
            packet.SerializeHeader(writer);
            packet.SerializeData(writer);
            writer.Flush();

            var length = writer.Position - offset;

            // Hash
            var hash = Crc32.Append(InitialHash, writer.Data, writer.Offset + offset, length);
            writer.WriteUInt(hash);
            writer.Flush();
        }

        public ushort GetTotalLength(IChannelPacket packet)
        {
            return (ushort)(1 + Crc32.HashLength + packet.Length);
        }
    }
}
