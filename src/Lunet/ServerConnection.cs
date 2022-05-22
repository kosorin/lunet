using Microsoft.Extensions.Logging;

namespace Lunet;

internal class ServerConnection : Connection
{
    private readonly UdpSocket _socket;


    private int _disposed;

    internal ServerConnection(UdpSocket socket, UdpEndPoint remoteEndPoint, ChannelFactory channelFactory, ILogger logger) : base(remoteEndPoint, channelFactory, logger)
    {
        _socket = socket;

        State = ConnectionState.Connected;
    }

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
        {
            return;
        }

        if (disposing)
        {
            State = ConnectionState.Disconnected;
            OnDisconnected();
        }

        base.Dispose(disposing);
    }


    public override void Connect()
    {
        throw new InvalidOperationException($"{nameof(ServerConnection)} is automatically connected by listener.");
    }


    private protected override void SendPacket(UdpPacket packet)
    {
        _socket.SendPacket(packet);
    }

    private protected override UdpPacket RentPacket()
    {
        var packet = _socket.RentPacket();
        packet.RemoteEndPoint = RemoteEndPoint;
        return packet;
    }
}
