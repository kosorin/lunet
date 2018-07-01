using System;

namespace Lure.Net.Packets.System
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class SystemPacketAttribute : Attribute
    {
        public SystemPacketAttribute(SystemPacketType packetType)
        {
            PacketType = packetType;
        }

        public SystemPacketType PacketType { get; }
    }
}
