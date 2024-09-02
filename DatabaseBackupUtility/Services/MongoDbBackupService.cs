namespace DatabaseBackupUtility.Configs;

public class MongoDbBackupService : IBackupService
{
    private readonly IDatabaseConnection _dbConnection;

    public MongoDbBackupService(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public void CreateBackup(string backupFilePath)
    {
        _dbConnection.Connect();
        _dbConnection.Backup(backupFilePath);
        _dbConnection.Disconnect();
    }
}