using System;

namespace Lure.Net
{
    public class NetSerializationException : NetException
    {
        internal NetSerializationException()
        {
        }

        internal NetSerializationException(string message)
            : base(message)
        {
        }

        internal NetSerializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
