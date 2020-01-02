using System.Collections.Generic;

namespace Lunet
{
    public interface IConnectionChannel
    {
        byte Id { get; }

        Connection Connection { get; }


        List<byte[]>? GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}
