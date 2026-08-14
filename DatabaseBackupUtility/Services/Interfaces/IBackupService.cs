using DatabaseBackupUtility.Models;

namespace DatabaseBackupUtility.Services.Interfaces;

public interface IBackupService
{
    Task<string?> CreateBackup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default);
}
