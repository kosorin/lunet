﻿using System.Runtime.InteropServices;

namespace Bur.Net
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct FloatingPoint
    {
        [FieldOffset(0)]
        public float Float;

        [FieldOffset(0)]
        public double Double;

        [FieldOffset(0)]
        public uint UInt;

        [FieldOffset(0)]
        public ulong ULong;


        [FieldOffset(0)]
        public byte Byte0;

        [FieldOffset(1)]
        public byte Byte1;

        [FieldOffset(2)]
        public byte Byte2;

        [FieldOffset(3)]
        public byte Byte3;

        [FieldOffset(4)]
        public byte Byte4;

        [FieldOffset(5)]
        public byte Byte5;

        [FieldOffset(6)]
        public byte Byte6;

        [FieldOffset(7)]
        public byte Byte7;
    }
}
