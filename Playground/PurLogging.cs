using Serilog;
using Serilog.Events;

namespace Pur
{
    public static class PurLogging
    {
        public static string FileName { get; } = "Log.log";

        public static string ShortTimeFormatString { get; } = "HH:mm:ss.fff";

        public static string LongTimeFormatString { get; } = "yyyy-MM-dd HH:mm:ss.fff";

        public static void Initialize(string name)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Verbose, $"{name}: [{{Timestamp:{ShortTimeFormatString}}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")
                .CreateLogger();
            Log.Logger.Information("Start");
        }
    }
}
