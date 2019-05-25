using Lunet.Data;

namespace Lunet.Channels
{
    public abstract class Message
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.

        public byte[] Data { get; set; }
        
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        public int Length => HeaderLength + DataLength;

        public virtual int HeaderLength => 0;

        public virtual int DataLength => sizeof(ushort) + Data.Length;

        public virtual void Deserialize(NetDataReader reader)
        {
            var length = reader.ReadUShort();
            var data = reader.ReadBytes(length);
            Data = data;
        }

        public virtual void Serialize(NetDataWriter writer)
        {
            writer.WriteUShort((ushort)Data.Length);
            writer.WriteBytes(Data);
        }
    }
}
