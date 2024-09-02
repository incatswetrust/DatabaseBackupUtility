namespace DatabaseBackupUtility.Configs;

public interface IDatabaseConnection
{
    bool TestConnection();
    void Connect();
    void Disconnect();
    // Adding a method to execute the backup command (data export)
    void Backup(string backupFilePath);
    // Adding a method to execute the recovery command (data import)
    void Restore(string backupFilePath);
}