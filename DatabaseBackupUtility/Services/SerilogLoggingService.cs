using DatabaseBackupUtility.Models;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DatabaseBackupUtility.Configs;

public class SerilogLoggingService : ILoggingService
{
    public SerilogLoggingService(LoggingSettings settings)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(restrictedToMinimumLevel: settings.ConsoleEnabled ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Fatal)
            .WriteTo.File(
                path: settings.Path,
                rollingInterval: Enum.Parse<RollingInterval>(settings.RollingInterval),
                retainedFileCountLimit: settings.RetainedFileCountLimit)
            .CreateLogger();
    }

    public void LogInfo(string message)
    {
        Log.Information(message);
    }

    public void LogWarning(string message)
    {
        Log.Warning(message);
    }

    public void LogError(string message)
    {
        Log.Error(message);
    }
}