using System;

namespace Lure.Net.Packets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    internal class PacketDataAttribute : Attribute
    {
        public PacketDataAttribute(PacketDataType dataType)
        {
            DataType = dataType;
        }

        public PacketDataType DataType { get; }
    }
}
