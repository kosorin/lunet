using System.Net.Sockets;

namespace Lure.Net
{
    public interface ISocketConfig
    {
        int? LocalPort { get; }

        AddressFamily AddressFamily { get; }

        bool DualMode { get; }

        int ReceiveBufferSize { get; }

        int SendBufferSize { get; }

        int PacketBufferSize { get; }
    }
}