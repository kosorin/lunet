using System;

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
    }
}
