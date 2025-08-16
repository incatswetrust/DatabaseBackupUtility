namespace DatabaseBackupUtility.Configs;

public class MongoDbBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public void CreateBackup(string backupFilePath)
    {
        dbConnection.Connect();
        dbConnection.Backup(backupFilePath);
        dbConnection.Disconnect();
    }
}