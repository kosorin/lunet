using Serilog;
using System;

namespace Bur.Common
{
    public static class Logging
    {
        public static string FileName { get; set; } = "Log.log";

        public static string ShortTimeFormatString { get; set; } = "HH:mm:ss.fff";

        public static string LongTimeFormatString { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        public static void Configure(Action<LoggerConfiguration> configurator)
        {
            var config = new LoggerConfiguration();
            configurator(config);
            Log.Logger = config.CreateLogger();
        }
    }
}
