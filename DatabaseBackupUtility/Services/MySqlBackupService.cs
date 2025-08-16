namespace DatabaseBackupUtility.Configs;

public class MySqlBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public void CreateBackup(string backupFilePath)
    {
        dbConnection.Connect();
        dbConnection.Backup(backupFilePath);
        dbConnection.Disconnect();
    }
}