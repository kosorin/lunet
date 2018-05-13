using Serilog;
using Serilog.Events;

namespace Bur
{
    public static class Logging
    {
        public static string FileName { get; } = "Log.log";

        public static string ShortTimeFormatString { get; } = "HH:mm:ss.fff";

        public static string LongTimeFormatString { get; } = "yyyy-MM-dd HH:mm:ss.fff";

        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithThreadId()

                .WriteTo.Console(
                    restrictedToMinimumLevel: GetConsoleLogLevel(),
                    outputTemplate: $"[{{Timestamp:{ShortTimeFormatString}}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")

                .WriteTo.Async(x => x.File(
                    path: FileName,
                    restrictedToMinimumLevel: GetFileLogLevel(),
                    outputTemplate: $"{{Timestamp:{LongTimeFormatString}}}|{{Level:u3}}|{{ThreadId}}|{{SourceContext}}|{{Message}}{{NewLine}}{{Exception}}"))

                .CreateLogger();

            LogEventLevel GetConsoleLogLevel()
            {
#if DEBUG
                return LogEventLevel.Verbose;
#else
                return LogEventLevel.Information;
#endif
            }

            LogEventLevel GetFileLogLevel()
            {
                return LogEventLevel.Verbose;
            }
        }
    }
}
