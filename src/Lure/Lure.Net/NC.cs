namespace Lure.Net
{
    /// <summary>
    /// Net Constants.
    /// </summary>
    internal static class NC
    {
        public const int BitsPerByte = 8;
        public const int BitsPerInt = 32;

        public const byte Zero = 0;
        public const byte One = 1;

        public const byte Byte = 0xFF;
        public const ushort Short = 0xFFFF;
        public const uint Int = 0xFFFF_FFFF;
        public const ulong Long = 0xFFFF_FFFF_FFFF_FFFF;

        public const int PacketHeaderSize = 9;
    }
}
