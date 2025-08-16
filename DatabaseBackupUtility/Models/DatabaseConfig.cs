namespace DatabaseBackupUtility.Models;


public class DatabaseConfig
{
    public string Type { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class NotificationConfig
{
    public string SlackWebhookUrl { get; set; } = string.Empty;
}

public class StorageConfig
{
    public string Type { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public CloudStorageConfig Cloud { get; set; } = new CloudStorageConfig();
}

public class CloudStorageConfig
{
    public string Provider { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}
