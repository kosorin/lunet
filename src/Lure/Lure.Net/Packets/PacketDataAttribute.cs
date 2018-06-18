using System;

namespace Lure.Net.Packets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class PacketDataAttribute : Attribute
    {
        public PacketDataAttribute(PacketDataType type)
        {
            Type = type;
        }

        public PacketDataType Type { get; }
    }
}
