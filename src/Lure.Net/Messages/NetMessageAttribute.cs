﻿using System;

namespace Lure.Net.Messages
{
    [Obsolete]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NetMessageAttribute : Attribute
    {
        public NetMessageAttribute(ushort messageTypeId)
        {
            MessageTypeId = messageTypeId;
        }

        public ushort MessageTypeId { get; }
    }
}
