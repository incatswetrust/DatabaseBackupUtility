using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class PostgreSqlBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public async Task<string?> CreateBackup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default)
    {
        await dbConnection.Connect();
        var position = await dbConnection.Backup(backupId, backupFilePath, type, parent, cancellationToken);
        await dbConnection.Disconnect();
        return position;
    }
}
