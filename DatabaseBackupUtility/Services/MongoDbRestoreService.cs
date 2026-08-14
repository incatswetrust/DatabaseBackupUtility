using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class MongoDbRestoreService(IDatabaseConnection dbConnection) : IRestoreService
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

    // mongorestore only accepts a single --oplogDir per invocation, so a chain with more than one
    // incremental/differential step is applied by restoring the full dump first, then merging the
    // oplog.bson files from every delta step (each is just a sequential BSON document stream, so
    // concatenation in chronological order produces one valid combined oplog) and replaying that
    // once.
    public async Task RestoreChain(IReadOnlyList<(string FilePath, BackupType Type)> chain, CancellationToken cancellationToken = default)
    {
        if (chain.Count == 0) return;

        var full = chain[0];
        await RestoreDatabase(full.FilePath, full.Type, cancellationToken);

        var deltas = chain.Skip(1).ToList();
        if (deltas.Count == 0) return;

        var mergedOplogDir = Path.Combine(Path.GetTempPath(), $"oplog_merge_{Guid.NewGuid():N}");
        Directory.CreateDirectory(mergedOplogDir);
        try
        {
            var mergedOplogPath = Path.Combine(mergedOplogDir, "oplog.bson");
            await using (var merged = File.Create(mergedOplogPath))
            {
                foreach (var delta in deltas)
                {
                    var oplogFile = Path.Combine(delta.FilePath, "oplog.bson");
                    if (!File.Exists(oplogFile)) continue;
                    await using var source = File.OpenRead(oplogFile);
                    await source.CopyToAsync(merged, cancellationToken);
                }
            }

            await dbConnection.Connect();
            try
            {
                await dbConnection.Restore(mergedOplogDir, BackupType.Incremental, cancellationToken);
                Console.WriteLine($"Applied {deltas.Count} incremental/differential step(s) from oplog history.");
            }
            finally
            {
                await dbConnection.Disconnect();
            }
        }
        finally
        {
            Directory.Delete(mergedOplogDir, recursive: true);
        }
    }
}
