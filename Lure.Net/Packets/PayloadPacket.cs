using System.Collections.Generic;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;

namespace Lure.Net.Packets
{
    internal class PayloadPacket : Packet
    {
        public override PacketType Type => PacketType.Payload;

        public byte[] Data { get; set; }

        protected override void DeserializeCore(INetDataReader reader)
        {
            Data = reader.ReadBytesToEnd();
            //Messages = new List<NetMessage>();
            //while (reader.Position < reader.Length)
            //{
            //    var id = reader.ReadUShort();
            //    var message = NetMessageManager.Create(id);
            //    if (message == null)
            //    {
            //        return;
            //    }

            //    reader.ReadMessage(message);
            //    reader.PadBits();
            //}
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            writer.WriteBytes(Data);
            //foreach (var message in Messages)
            //{
            //    writer.WriteMessage(message);
            //    writer.PadBits();
            //}
        }
    }
}
