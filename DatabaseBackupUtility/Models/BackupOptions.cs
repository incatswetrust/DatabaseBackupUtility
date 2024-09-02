namespace DatabaseBackupUtility.Models;

public class BackupOptions
{
    public string BackupFilePath { get; set; } 
    public string BackupType { get; set; } 
    public bool CompressBackup { get; set; }
}