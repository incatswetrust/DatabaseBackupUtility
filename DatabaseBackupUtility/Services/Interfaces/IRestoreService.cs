using DatabaseBackupUtility.Models;

namespace DatabaseBackupUtility.Services.Interfaces;

public interface IRestoreService
{
    Task RestoreDatabase(string backupFilePath, BackupType type = BackupType.Full, IReadOnlyList<string>? targets = null,
        CancellationToken cancellationToken = default);

    // Applies a resolved full -> incremental/differential chain in order. The default
    // implementation just replays RestoreDatabase per step, which is correct for engines whose
    // incremental/differential files are directly appliable the same way as a full backup (e.g.
    // SQL statements). MongoDB overrides this, since applying oplog-based deltas requires a
    // single combined mongorestore --oplogReplay call rather than one call per step.
    async Task RestoreChain(IReadOnlyList<(string FilePath, BackupType Type)> chain, IReadOnlyList<string>? targets = null,
        CancellationToken cancellationToken = default)
    {
        foreach (var step in chain)
            await RestoreDatabase(step.FilePath, step.Type, targets, cancellationToken);
    }
}
