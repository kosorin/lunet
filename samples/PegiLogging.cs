using Lure.Net.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Reflection;

namespace Pegi
{
    public static class PegiLogging
    {
        public static string FileName { get; set; } = "Log.log";

        public static string ShortTimeFormatString { get; set; } = "HH:mm:ss.fff";

        public static string LongTimeFormatString { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";


        public static void Configure(string name)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var outputTemplate = $"{name}: [{{Timestamp:{ShortTimeFormatString}}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}";
            var config = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console(LogEventLevel.Verbose, outputTemplate)
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
                ;
            Log.Logger = config.CreateLogger();
            Log.Logger.Information("{AppName} v{Version}", name, version);
        }
    }
}
