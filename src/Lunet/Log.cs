/*
using Microsoft.Extensions.Logging;
using System;

namespace Lunet
{
    internal static class Log
    {
        private static readonly Action<ILogger, string, Exception?> _hello;

        static Log()
        {
            _hello = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(Hello)), "Hello {Name}!");
        }

        public static ILogger Logger { get; set; }

        public static void Hello(string name) => _hello.Invoke(Logger, name, null);
    }
}
*/
