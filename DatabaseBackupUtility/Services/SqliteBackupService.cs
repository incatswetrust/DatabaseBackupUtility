using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class SqliteBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public async Task CreateBackup(string backupFilePath, CancellationToken cancellationToken = default)
    {
        await dbConnection.Connect();
        await dbConnection.Backup(backupFilePath, cancellationToken);
        await dbConnection.Disconnect();
    }
}
