using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class PostgreSqlRestoreService(IDatabaseConnection dbConnection) : IRestoreService
{
    public async Task RestoreDatabase(string backupFilePath, CancellationToken cancellationToken = default)
    {
        await dbConnection.Connect();
        try
        {
            await dbConnection.Restore(backupFilePath, cancellationToken);
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