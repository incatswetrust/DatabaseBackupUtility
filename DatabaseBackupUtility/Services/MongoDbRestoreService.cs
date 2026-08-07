namespace DatabaseBackupUtility.Configs;

public class MongoDbRestoreService(IDatabaseConnection dbConnection) : IRestoreService
{
    public async Task RestoreDatabase(string backupFilePath)
    {
        await dbConnection.Connect();
        try
        {
            await dbConnection.Restore(backupFilePath);
            Console.WriteLine($"Database restored successfully from {backupFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during database restore: {ex.Message}");
            throw;
        }
        finally
        {
            await dbConnection.Disconnect();
        }
    }
}