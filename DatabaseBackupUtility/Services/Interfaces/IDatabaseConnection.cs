using DatabaseBackupUtility.Models;

namespace DatabaseBackupUtility.Services.Interfaces;

public interface IDatabaseConnection
{
    Task<bool> TestConnection();
    Task Connect();
    Task Disconnect();

    // Returns an opaque, engine-specific checkpoint (or null when the engine tracks position
    // itself, e.g. via a server-side replication slot) to be stored on the resulting backup's
    // manifest, so a later Incremental/Differential backup can resume from it via `parent`.
    Task<string?> Backup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default);

    Task Restore(string backupFilePath, BackupType type = BackupType.Full, CancellationToken cancellationToken = default);
}
