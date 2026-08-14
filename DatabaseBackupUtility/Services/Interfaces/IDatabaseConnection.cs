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

    // `targets` optionally restricts the restore to specific tables (MySQL/PostgreSQL) or a
    // single collection (MongoDB); null/empty restores everything in the backup, as before.
    Task Restore(string backupFilePath, BackupType type = BackupType.Full, IReadOnlyList<string>? targets = null,
        CancellationToken cancellationToken = default);
}
