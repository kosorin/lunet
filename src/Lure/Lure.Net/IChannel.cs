using Lure.Net.Data;
using System.Collections.Generic;

namespace Lure.Net
{
    public interface IChannel
    {
        byte Id { get; }

        Connection Connection { get; }


        void ProcessIncomingPacket(NetDataReader reader);

        IList<IPacket> CollectOutgoingPackets();

        IList<byte[]> GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}
