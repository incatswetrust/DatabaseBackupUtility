namespace DatabaseBackupUtility.Configs;

public class MySqlRestoreService(IDatabaseConnection dbConnection) : IRestoreService
{
    public async Task RestoreDatabase(string backupFilePath)
    {
        dbConnection.Connect();
        try
        {
            dbConnection.Restore(backupFilePath);
            Console.WriteLine($"Database restored successfully from {backupFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during database restore: {ex.Message}");
            throw;
        }
        finally
        {
            dbConnection.Disconnect();
        }
    }
}