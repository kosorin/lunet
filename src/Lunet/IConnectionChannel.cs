using System.Collections.Generic;

namespace Lunet
{
    public interface IConnectionChannel
    {
        byte Id { get; }

        Connection Connection { get; }


        IList<byte[]>? GetReceivedMessages();

        void SendMessage(byte[] data);
    }
}
