namespace DatabaseBackupUtility.Models;

public class LoggingSettings
{
    public string Path { get; set; } = "logs/log.txt";
    public string RollingInterval { get; set; } = "Day";
    public int RetainedFileCountLimit { get; set; }
    public bool ConsoleEnabled { get; set; }
}