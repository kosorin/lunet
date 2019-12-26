﻿using MessagePack;

namespace Pegi
{
    [MessagePackObject]
    public class DebugMessage
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Text { get; set; }

        [Key(2)]
        public byte[] Data { get; set; } = new byte[200];

        public override string ToString()
        {
            return $"ID {Id,6} > '{Text}'";
        }
    }
}
