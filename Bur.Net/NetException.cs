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

        /// <summary>
        /// Throws an exception (in DEBUG only) if condition is false.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new NetException(message);
            }
        }

        /// <summary>
        /// Throws an exception (in DEBUG only) if condition is false.
        /// </summary>
        [Conditional("DEBUG")]
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new NetException();
            }
        }
    }
}
