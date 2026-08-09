using DatabaseBackupUtility.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class SerilogLoggingService : ILoggingService
{
    public SerilogLoggingService(LoggingSettings settings)
    {
        var logDirectory = Path.GetDirectoryName(settings.Path);
        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

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