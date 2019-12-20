using Lunet.Common;
using Lunet.Data;
using System;

namespace Lunet
{
    public abstract class ProtocolPacket
    {
        static ProtocolPacket()
        {
            VersionHash = Crc32.Compute(Version.ToByteArray());
        }

        public static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        protected static uint VersionHash { get; }


        public byte ChannelId { get; set; }
    }

    public class IncomingProtocolPacket : ProtocolPacket
    {
        public IncomingProtocolPacket(NetDataReader reader)
        {
            Reader = reader;
        }

        public NetDataReader Reader { get; }

        public bool Read(int offset, int length)
        {
            Reader.Reset(offset, length);

            if (!Crc32.Check(VersionHash, Reader.GetSpan()))
            {
                return false;
            }

            ChannelId = Reader.ReadByte();

            Reader.ResetRelative(sizeof(byte), Crc32.HashLength);

            return true;
        }
    }

    public class OutgoingProtocolPacket : ProtocolPacket
    {
        public IChannelPacket? ChannelPacket { get; set; }

        public void Write(NetDataWriter writer)
        {
            if (ChannelPacket == null)
            {
                throw new NullReferenceException();
            }

            writer.Reset();

            // Packet
            writer.WriteByte(ChannelId);
            ChannelPacket.SerializeHeader(writer);
            ChannelPacket.SerializeData(writer);
            writer.Flush();

            // Hash
            var hash = Crc32.Append(VersionHash, writer.Data, writer.Offset, writer.Position);
            writer.WriteUInt(hash);
            writer.Flush();
        }
    }
}
