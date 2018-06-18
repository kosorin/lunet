using Lure.Net.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel
    {
        private readonly NetConnection _connection;

        protected NetChannel(NetConnection connection)
        {
            _connection = connection;
        }

        protected abstract void PrepareOutgoingPacket(Packet packet);
    }
}
