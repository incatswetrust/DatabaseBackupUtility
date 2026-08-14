using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class SqliteRestoreService(IDatabaseConnection dbConnection) : IRestoreService
{
    public async Task RestoreDatabase(string backupFilePath, BackupType type = BackupType.Full, CancellationToken cancellationToken = default)
    {
        await dbConnection.Connect();
        try
        {
            await dbConnection.Restore(backupFilePath, type, cancellationToken);
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
