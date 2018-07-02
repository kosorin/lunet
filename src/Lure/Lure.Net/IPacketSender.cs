using Lure.Net.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lure.Net
{
    internal interface IPacketSender
    {
        void SendPacket(Packet packet);
    }
}
