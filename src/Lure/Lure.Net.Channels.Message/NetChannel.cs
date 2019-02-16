using Lure.Net.Data;
using System.Collections.Generic;

namespace Lure.Net.Channels.Message
{
    public abstract class NetChannel : IChannel
    {
        protected NetChannel(byte id, Connection connection)
        {
            Id = id;
            Connection = connection;
        }


        public byte Id { get; }

        public Connection Connection { get; }


        public abstract void ProcessIncomingPacket(NetDataReader reader);

        public abstract IList<IPacket> CollectOutgoingPackets();

        public abstract IList<byte[]> GetReceivedMessages();

        public abstract void SendMessage(byte[] data);
    }
}
