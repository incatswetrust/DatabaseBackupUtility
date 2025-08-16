namespace DatabaseBackupUtility.Configs;

public class MongoDbRestoreService(IDatabaseConnection dbConnection) : IRestoreService
{
    public void RestoreDatabase(string backupFilePath)
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