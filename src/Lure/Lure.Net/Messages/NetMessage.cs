﻿using System;
using Lure.Net.Data;

namespace Lure.Net.Messages
{
    public abstract class NetMessage
    {
        internal ushort TypeId { get; set; }

        public void Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read to create a message

            DeserializeCore(reader);
        }

        public void Serialize(INetDataWriter writer)
        {
            writer.WriteUShort(TypeId);

            SerializeCore(writer);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
