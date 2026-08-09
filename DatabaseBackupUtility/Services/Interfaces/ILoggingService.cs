namespace DatabaseBackupUtility.Services.Interfaces;

public interface ILoggingService
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
}