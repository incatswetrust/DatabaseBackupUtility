namespace DatabaseBackupUtility.Configs;

public class PostgreSqlBackupService : IBackupService
{
    private readonly IDatabaseConnection _dbConnection;

    public PostgreSqlBackupService(IDatabaseConnection dbConnection)
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