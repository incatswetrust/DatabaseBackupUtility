namespace DatabaseBackupUtility.Configs;

public class MySqlBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public async Task CreateBackup(string backupFilePath)
    {
        dbConnection.Connect();
        dbConnection.Backup(backupFilePath);
        dbConnection.Disconnect();
    }
}