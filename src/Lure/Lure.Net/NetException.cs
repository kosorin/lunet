using System;

namespace Lure.Net
{
    public class NetException : Exception
    {
        internal NetException()
        {
        }

        internal NetException(string message)
            : base(message)
        {
        }

        internal NetException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
