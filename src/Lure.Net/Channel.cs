using Lure.Net.Data;
using System.Collections.Generic;

namespace Lure.Net
{
    public abstract class Channel : IChannel
    {
        protected Channel(byte id, IConnection connection)
        {
            Id = id;
            Connection = connection;
        }


        public byte Id { get; }

        public IConnection Connection { get; }


        public abstract void HandleIncomingPacket(NetDataReader reader);

        public abstract IList<IChannelPacket> CollectOutgoingPackets();

        public abstract IList<byte[]> GetReceivedMessages();

        public abstract void SendMessage(byte[] data);
    }
}
