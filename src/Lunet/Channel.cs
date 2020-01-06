using Lunet.Data;
using System;
using System.Collections.Generic;

namespace Lunet
{
    public abstract class Channel
    {
        protected Channel(byte id, Connection connection)
        {
            Id = id;
            Connection = connection;
        }


        public byte Id { get; }

        public Connection Connection { get; }


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
}
