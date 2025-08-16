namespace DatabaseBackupUtility.Configs;

public class PostgreSqlBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public async Task CreateBackup(string backupFilePath)
    {
        await dbConnection.Connect();
        await dbConnection.Backup(backupFilePath);
        await dbConnection.Disconnect();
    }
}