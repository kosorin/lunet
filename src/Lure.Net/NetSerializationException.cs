using System;

namespace Lunet
{
    public class NetSerializationException : NetException
    {
        public NetSerializationException()
        {
        }

        public NetSerializationException(string message)
            : base(message)
        {
        }

        public NetSerializationException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
