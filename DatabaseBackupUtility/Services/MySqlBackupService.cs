namespace DatabaseBackupUtility.Configs;

public class MySqlBackupService : IBackupService
{
    private readonly IDatabaseConnection _dbConnection;

    public MySqlBackupService(IDatabaseConnection dbConnection)
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