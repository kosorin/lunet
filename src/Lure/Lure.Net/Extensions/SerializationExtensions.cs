﻿using Lure.Net.Data;

namespace Lure.Net.Extensions
{
    internal static class SerializationExtensions
    {
        public static SeqNo ReadSeqNo(this INetDataReader reader)
        {
            return new SeqNo(reader.ReadUShort());
        }

        public static void WriteSeqNo(this INetDataWriter writer, SeqNo seq)
        {
            writer.WriteUShort(seq.Value);
        }

        public static byte[] ReadByteArray(this INetDataReader reader)
        {
            var length = reader.ReadUShort();
            var array = reader.ReadBytes(length);
            return array;
        }

        public static void WriteByteArray(this INetDataWriter writer, byte[] array)
        {
            writer.WriteUShort((ushort)array.Length);
            writer.WriteBytes(array);
        }
    }
}