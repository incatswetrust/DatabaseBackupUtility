using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class SqliteBackupRestoreIntegrationTests
{
    [Fact]
    public async Task CreateBackup_ConnectsBacksUpAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new SqliteBackupService(connection);

        await backupService.CreateBackup("/tmp/backup.db");

        Assert.Equal(["Connect", "Backup", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.db", connection.BackupFilePathReceived);
    }

    [Fact]
    public async Task CreateBackup_PassesCancellationTokenThrough()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new SqliteBackupService(connection);
        using var cts = new CancellationTokenSource();

        await backupService.CreateBackup("/tmp/backup.db", cts.Token);

        Assert.Equal(cts.Token, connection.BackupTokenReceived);
    }

    [Fact]
    public async Task RestoreDatabase_ConnectsRestoresAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var restoreService = new SqliteRestoreService(connection);

        await restoreService.RestoreDatabase("/tmp/backup.db");

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.db", connection.RestoreFilePathReceived);
    }

    [Fact]
    public async Task RestoreDatabase_DisconnectsEvenWhenRestoreFails()
    {
        var connection = new FakeDatabaseConnection { ThrowOnRestore = true };
        var restoreService = new SqliteRestoreService(connection);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => restoreService.RestoreDatabase("/tmp/backup.db"));

        Assert.Equal(["Connect", "Restore", "Disconnect"], connection.Calls);
    }

    [Fact]
    public async Task BackupThenRestore_RoundTripsThroughTheSameConnection()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new SqliteBackupService(connection);
        var restoreService = new SqliteRestoreService(connection);

        await backupService.CreateBackup("/tmp/backup.db");
        await restoreService.RestoreDatabase("/tmp/backup.db");

        Assert.Equal(["Connect", "Backup", "Disconnect", "Connect", "Restore", "Disconnect"], connection.Calls);
    }
}
