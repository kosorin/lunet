using System.Reflection;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Pegi;

public static class PegiLogging
{
    public static string FileName { get; set; } = "Log.log";

    public static string ShortTimeFormatString { get; set; } = "HH:mm:ss.fff";

    public static string LongTimeFormatString { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    public static Microsoft.Extensions.Logging.ILogger Configure(string name)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        var outputTemplate = $"{{SourceContext}}: [{{Timestamp:{ShortTimeFormatString}}} {{Level:u3}}] [{{ThreadId}}] {{Message:lj}}{{NewLine}}{{Exception}}";
        var config = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console(LogEventLevel.Verbose, outputTemplate)
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
            ;
        Log.Logger = config.CreateLogger().ForContext("SourceContext", name);
        Log.Logger.Information("{AppName} v{Version}", name, version);

        using var loggerFactory = new SerilogLoggerFactory().AddSerilog();
        return loggerFactory.CreateLogger(name);
    }
}
