namespace DatabaseBackupUtility.Models;

public class RestoreOptions
{
    public string BackupFilePath { get; set; } 
    public string TargetDatabaseName { get; set; } 
    public bool OverwriteExisting { get; set; }
}