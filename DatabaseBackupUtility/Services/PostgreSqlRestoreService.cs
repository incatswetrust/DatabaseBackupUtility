namespace DatabaseBackupUtility.Configs;

public class PostgreSqlRestoreService(IDatabaseConnection dbConnection) : IRestoreService
{
    public async Task RestoreDatabase(string backupFilePath)
    {
        await Task.Run(() =>
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
        });
    }
}