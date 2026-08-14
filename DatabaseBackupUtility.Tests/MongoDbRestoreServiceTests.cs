using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Tests;

public class MongoDbRestoreServiceTests
{
    [Fact]
    public async Task RestoreDatabase_ForwardsSelectiveCollectionTargetToTheConnection()
    {
        var connection = new FakeDatabaseConnection();
        var restoreService = new MongoDbRestoreService(connection);

        await restoreService.RestoreDatabase("/tmp/backup", targets: ["orders"]);

        Assert.Equal(["orders"], connection.RestoreTargetsReceived);
    }

    [Fact]
    public async Task RestoreChain_WithOnlyAFullStep_RestoresJustTheFullBackup()
    {
        var connection = new FakeDatabaseConnection();
        IRestoreService restoreService = new MongoDbRestoreService(connection);

        await restoreService.RestoreChain([("/tmp/full", BackupType.Full)]);

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/full", connection.RestoreFilePathReceived);
    }

    [Fact]
    public async Task RestoreChain_WithIncrementalSteps_MergesOplogFilesAndAppliesThemInOneCall()
    {
        var fullDir = Path.Combine(Path.GetTempPath(), $"mongo_full_{Guid.NewGuid():N}");
        var inc1Dir = Path.Combine(Path.GetTempPath(), $"mongo_inc1_{Guid.NewGuid():N}");
        var inc2Dir = Path.Combine(Path.GetTempPath(), $"mongo_inc2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(fullDir);
        Directory.CreateDirectory(inc1Dir);
        Directory.CreateDirectory(inc2Dir);
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(inc1Dir, "oplog.bson"), [1, 2, 3]);
            await File.WriteAllBytesAsync(Path.Combine(inc2Dir, "oplog.bson"), [4, 5]);

            byte[]? mergedOplogBytes = null;
            var connection = new FakeDatabaseConnection
            {
                OnRestore = path =>
                {
                    var oplogFile = Path.Combine(path, "oplog.bson");
                    if (File.Exists(oplogFile))
                        mergedOplogBytes = File.ReadAllBytes(oplogFile);
                }
            };
            IRestoreService restoreService = new MongoDbRestoreService(connection);

            await restoreService.RestoreChain([
                (fullDir, BackupType.Full),
                (inc1Dir, BackupType.Incremental),
                (inc2Dir, BackupType.Incremental)
            ]);

            // Full restore, then one more Connect/Restore/Disconnect cycle for the merged oplog replay.
            Assert.Equal(
                ["Connect", "Restore", "Disconnect", "Connect", "Restore", "Disconnect"],
                connection.Calls);
            Assert.Equal(BackupType.Incremental, connection.RestoreTypeReceived);
            Assert.NotEqual(fullDir, connection.RestoreFilePathReceived);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, mergedOplogBytes);
        }
        finally
        {
            Directory.Delete(fullDir, recursive: true);
            Directory.Delete(inc1Dir, recursive: true);
            Directory.Delete(inc2Dir, recursive: true);
        }
    }
}
