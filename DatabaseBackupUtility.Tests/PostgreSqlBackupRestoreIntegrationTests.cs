using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class PostgreSqlBackupRestoreIntegrationTests
{
    [Fact]
    public async Task CreateBackup_ConnectsBacksUpAndDisconnectsInOrder()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new PostgreSqlBackupService(connection);

        await backupService.CreateBackup("/tmp/backup.sql");

        Assert.Equal(["Connect", "Backup", "Disconnect"], connection.Calls);
        Assert.Equal("/tmp/backup.sql", connection.BackupFilePathReceived);
    }

    [Fact]
    public async Task CreateBackup_PassesCancellationTokenThrough()
    {
        var connection = new FakeDatabaseConnection();
        var backupService = new PostgreSqlBackupService(connection);
        using var cts = new CancellationTokenSource();

        await backupService.CreateBackup("/tmp/backup.sql", cts.Token);

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

        await backupService.CreateBackup("/tmp/backup.sql");
        await restoreService.RestoreDatabase("/tmp/backup.sql");

        Assert.Equal(["Connect", "Backup", "Disconnect", "Connect", "Restore", "Disconnect"], connection.Calls);
    }
}
