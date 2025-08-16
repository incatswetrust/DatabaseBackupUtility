namespace DatabaseBackupUtility.Models;

public class CloudStorageConfig
{
    public string Provider { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}