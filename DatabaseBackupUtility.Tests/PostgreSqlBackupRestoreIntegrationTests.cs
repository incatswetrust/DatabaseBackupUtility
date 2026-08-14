using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Tests;

public class PostgreSqlBackupRestoreIntegrationTests
{
    [Fact]
    public async Task CreateBackup_ConnectsBacksUpAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new PostgreSqlBackupService(connection);

        await backupService.CreateBackup("backup-1", "/tmp/backup.sql");

        Assert.Equal(["Connect", "Backup", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.sql", connection.BackupFilePathReceived);
    }

    [Fact]
    public async Task CreateBackup_PassesCancellationTokenThrough()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new PostgreSqlBackupService(connection);
        using var cts = new CancellationTokenSource();

        await backupService.CreateBackup("backup-1", "/tmp/backup.sql", cancellationToken: cts.Token);

        Assert.Equal(cts.Token, connection.BackupTokenReceived);
    }

    [Fact]
    public async Task RestoreDatabase_ConnectsRestoresAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var restoreService = new PostgreSqlRestoreService(connection);

        await restoreService.RestoreDatabase("/tmp/backup.sql");

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.sql", connection.RestoreFilePathReceived);
    }

    [Fact]
    public async Task RestoreDatabase_DisconnectsEvenWhenRestoreFails()
    {
        var connection = new FakeDatabaseConnection { ThrowOnRestore = true };
        var restoreService = new PostgreSqlRestoreService(connection);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => restoreService.RestoreDatabase("/tmp/backup.sql"));

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
    }

    [Fact]
    public async Task BackupThenRestore_RoundTripsThroughTheSameConnection()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new PostgreSqlBackupService(connection);
        var restoreService = new PostgreSqlRestoreService(connection);

        await backupService.CreateBackup("backup-1", "/tmp/backup.sql");
        await restoreService.RestoreDatabase("/tmp/backup.sql");

        Assert.Equal(["Connect", "Backup", "Disconnect", "Connect", "Restore", "Disconnect"], connection.Calls);
    }

    [Fact]
    public async Task FullThenDifferentialThenRestoreChain_AppliesFullAndDifferentialOnly()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new PostgreSqlBackupService(connection);
        IRestoreService restoreService = new PostgreSqlRestoreService(connection);

        await backupService.CreateBackup("full-1", "/tmp/full.sql");
        var parent = new BackupParent("full-1", null);
        await backupService.CreateBackup("diff-1", "/tmp/diff1.sql", BackupType.Differential, parent);
        Assert.Equal(BackupType.Differential, connection.BackupTypeReceived);
        Assert.Equal(parent, connection.ParentReceived);

        await restoreService.RestoreChain([
            ("/tmp/full.sql", BackupType.Full),
            ("/tmp/diff1.sql", BackupType.Differential)
        ]);

        Assert.Equal("/tmp/diff1.sql", connection.RestoreFilePathReceived);
        Assert.Equal(BackupType.Differential, connection.RestoreTypeReceived);
    }

    [Fact]
    public async Task RestoreDatabase_ForwardsSelectiveTableTargetToTheConnection()
    {
        var connection = new FakeDatabaseConnection();
        var restoreService = new PostgreSqlRestoreService(connection);

        await restoreService.RestoreDatabase("/tmp/backup.sql", targets: ["orders"]);

        Assert.Equal(["orders"], connection.RestoreTargetsReceived);
    }
}
