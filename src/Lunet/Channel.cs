using Lunet.Data;
using System.Collections.Generic;

namespace Lunet
{
    public abstract class Channel : IChannel
    {
        protected Channel(byte id, Connection connection)
        {
            Id = id;
            Connection = connection;
        }


        public byte Id { get; }

        public Connection Connection { get; }


        public abstract void HandleIncomingPacket(NetDataReader reader);

        public abstract IList<IChannelPacket>? CollectOutgoingPackets();

        public abstract IList<byte[]>? GetReceivedMessages();

        public abstract void SendMessage(byte[] data);
    }
}
