using Lure.Net.Data;
using System.Collections.Generic;

namespace Lure.Net
{
    public interface IChannel
    {
        byte Id { get; }

        IConnection Connection { get; }


        void HandleIncomingPacket(NetDataReader reader);

        IList<IChannelPacket> CollectOutgoingPackets();

        IList<byte[]> GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}
