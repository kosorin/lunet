using System;

namespace Lure.Net
{
    public class NetException : Exception
    {
        public NetException()
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
