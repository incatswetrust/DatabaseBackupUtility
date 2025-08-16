namespace DatabaseBackupUtility.Configs;

public interface IDatabaseConnection
{
    Task<bool> TestConnection();
    Task Connect();
    Task Disconnect();
    // Adding a method to execute the backup command (data export)
    Task Backup(string backupFilePath);
    // Adding a method to execute the recovery command (data import)
    Task Restore(string backupFilePath);
}