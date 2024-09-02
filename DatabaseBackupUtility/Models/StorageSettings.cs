namespace DatabaseBackupUtility.Models;

public class StorageSettings
{
    public string StorageType { get; set; } 
    public string LocalPath { get; set; } 
    public string CloudBucketName { get; set; } 
    public string CloudProvider { get; set; }
}