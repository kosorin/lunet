using System;

namespace Lure.Net.Packets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class PacketAttribute : Attribute
    {
        public PacketAttribute(PacketType type)
        {
            Type = type;
        }

        public PacketType Type { get; }
    }
}
