using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Tests;

public class MySqlBackupRestoreIntegrationTests
{
    [Fact]
    public async Task CreateBackup_ConnectsBacksUpAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new MySqlBackupService(connection);

        await backupService.CreateBackup("backup-1", "/tmp/backup.sql");

        Assert.Equal(["Connect", "Backup", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.sql", connection.BackupFilePathReceived);
    }

    [Fact]
    public async Task CreateBackup_PassesCancellationTokenThrough()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new MySqlBackupService(connection);
        using var cts = new CancellationTokenSource();

        await backupService.CreateBackup("backup-1", "/tmp/backup.sql", cancellationToken: cts.Token);

        Assert.Equal(cts.Token, connection.BackupTokenReceived);
    }

    [Fact]
    public async Task RestoreDatabase_ConnectsRestoresAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var restoreService = new MySqlRestoreService(connection);

        await restoreService.RestoreDatabase("/tmp/backup.sql");

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.sql", connection.RestoreFilePathReceived);
    }

    [Fact]
    public async Task RestoreDatabase_DisconnectsEvenWhenRestoreFails()
    {
        var connection = new FakeDatabaseConnection { ThrowOnRestore = true };
        var restoreService = new MySqlRestoreService(connection);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => restoreService.RestoreDatabase("/tmp/backup.sql"));

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
    }

    [Fact]
    public async Task BackupThenRestore_RoundTripsThroughTheSameConnection()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new MySqlBackupService(connection);
        var restoreService = new MySqlRestoreService(connection);

        await backupService.CreateBackup("backup-1", "/tmp/backup.sql");
        await restoreService.RestoreDatabase("/tmp/backup.sql");

        Assert.Equal(["Connect", "Backup", "Disconnect", "Connect", "Restore", "Disconnect"], connection.Calls);
    }

    [Fact]
    public async Task FullThenIncrementalThenRestoreChain_AppliesEachStepInOrder()
    {
        var connection = new FakeDatabaseConnection { PositionToReturn = "mysql-bin.000001:100" };
        var backupService = new MySqlBackupService(connection);
        IRestoreService restoreService = new MySqlRestoreService(connection);

        var fullPosition = await backupService.CreateBackup("full-1", "/tmp/full.sql");
        Assert.Equal(BackupType.Full, connection.BackupTypeReceived);
        Assert.Null(connection.ParentReceived);

        connection.PositionToReturn = "mysql-bin.000001:250";
        var parent = new BackupParent("full-1", fullPosition);
        await backupService.CreateBackup("inc-1", "/tmp/inc1.sql", BackupType.Incremental, parent);
        Assert.Equal(BackupType.Incremental, connection.BackupTypeReceived);
        Assert.Equal(parent, connection.ParentReceived);

        await restoreService.RestoreChain([
            ("/tmp/full.sql", BackupType.Full),
            ("/tmp/inc1.sql", BackupType.Incremental)
        ]);

        Assert.Equal("/tmp/inc1.sql", connection.RestoreFilePathReceived);
        Assert.Equal(BackupType.Incremental, connection.RestoreTypeReceived);
    }

    [Fact]
    public async Task RestoreDatabase_ForwardsSelectiveTableTargetToTheConnection()
    {
        var connection = new FakeDatabaseConnection();
        var restoreService = new MySqlRestoreService(connection);

        await restoreService.RestoreDatabase("/tmp/backup.sql", targets: ["orders"]);

        Assert.Equal(["orders"], connection.RestoreTargetsReceived);
    }
}
