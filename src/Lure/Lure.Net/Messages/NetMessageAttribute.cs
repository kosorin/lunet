﻿using System;

namespace Lure.Net.Messages
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NetMessageAttribute : Attribute
    {
        public const ushort UserStartTypeId = 0b1000;

        public NetMessageAttribute(ushort typeId)
        {
            Id = typeId;
        }

        public ushort Id { get; }
    }
}