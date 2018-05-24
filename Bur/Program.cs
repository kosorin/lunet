using Bur.Common;
using Serilog;
using Serilog.Events;

namespace Bur
{
    internal static class Program
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(Program));

        private static void Main()
        {
            Logging.Configure(LoggerConfigurator);
        }

        private static void LoggerConfigurator(LoggerConfiguration config)
        {
            config
                .MinimumLevel.Verbose()
                .Enrich.WithThreadId()

                .WriteTo.Console(
                    restrictedToMinimumLevel: GetConsoleLogLevel(),
                    outputTemplate: $"[{{Timestamp:{Logging.ShortTimeFormatString}}} {{Level:u3}}] {{Message:lj}}{{NewLine}}{{Exception}}")

                .WriteTo.Async(x => x.File(
                    path: Logging.FileName,
                    restrictedToMinimumLevel: GetFileLogLevel(),
                    outputTemplate: $"{{Timestamp:{Logging.LongTimeFormatString}}}|{{Level:u3}}|{{ThreadId}}|{{SourceContext}}|{{Message}}{{NewLine}}{{Exception}}"))

                ;

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
