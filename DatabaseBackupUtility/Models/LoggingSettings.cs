namespace DatabaseBackupUtility.Models;

public class LoggingSettings
{
    public string Path { get; set; }
    public string RollingInterval { get; set; }
    public int RetainedFileCountLimit { get; set; }
    public bool ConsoleEnabled { get; set; }
}