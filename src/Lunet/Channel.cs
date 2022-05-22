using Lunet.Data;
using Microsoft.Extensions.Logging;

// TODO: List -> Array if possible

namespace Lunet;

public abstract class Channel
{
    protected Channel(byte id, Connection connection)
    {
        Id = id;
        Connection = connection;
        Logger = connection.Logger;
    }


    public byte Id { get; }

    public Connection Connection { get; }

    protected ILogger Logger { get; }


    public abstract List<byte[]>? GetReceivedMessages();

    public abstract void SendMessage(byte[] data);


    internal abstract void HandleIncomingPacket(NetDataReader reader);

    internal abstract List<ChannelPacket>? CollectOutgoingPackets();
}

public abstract class Channel<TPacket> : Channel
    where TPacket : ChannelPacket
{
    protected Channel(byte id, Connection connection) : base(id, connection)
    {
    }

    protected abstract Func<TPacket> PacketActivator { get; }
}

public delegate Channel ChannelConstructor(byte id, Connection connection);
