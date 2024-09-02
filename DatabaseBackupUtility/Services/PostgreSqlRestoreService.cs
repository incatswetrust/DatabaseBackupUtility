namespace DatabaseBackupUtility.Configs;

public class PostgreSqlRestoreService : IRestoreService
{
    private readonly IDatabaseConnection _dbConnection;

    public PostgreSqlRestoreService(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public void RestoreDatabase(string backupFilePath)
    {
        _dbConnection.Connect();
        try
        {
            _dbConnection.Restore(backupFilePath);
            Console.WriteLine($"Database restored successfully from {backupFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during database restore: {ex.Message}");
            throw;
        }
        finally
        {
            _dbConnection.Disconnect();
        }
    }
}