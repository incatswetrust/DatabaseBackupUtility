namespace DatabaseBackupUtility.Models;

public class Storage
{
    public string Type { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public CloudStorageConfig Cloud { get; set; } = new CloudStorageConfig();
}