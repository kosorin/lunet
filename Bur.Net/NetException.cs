using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Bur.Net
{
    public sealed class NetException : Exception
    {
        public NetException()
            : base()
        {
        }

        public NetException(string message)
            : base(message)
        {
        }

        public NetException(string message, Exception inner)
            : base(message, inner)
        {
        }

        private NetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
