using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Tests;

internal class FakeDatabaseConnection : IDatabaseConnection
{
    public List<string> Calls { get; } = [];
    public string? BackupFilePathReceived { get; private set; }
    public string? RestoreFilePathReceived { get; private set; }
    public CancellationToken BackupTokenReceived { get; private set; }
    public CancellationToken RestoreTokenReceived { get; private set; }
    public bool ThrowOnBackup { get; set; }
    public bool ThrowOnRestore { get; set; }
    public bool TestConnectionResult { get; set; } = true;

    public Task<bool> TestConnection()
    {
        Calls.Add(nameof(TestConnection));
        return Task.FromResult(TestConnectionResult);
    }

    public Task Connect()
    {
        Calls.Add(nameof(Connect));
        return Task.CompletedTask;
    }

    public Task Disconnect()
    {
        Calls.Add(nameof(Disconnect));
        return Task.CompletedTask;
    }

    public Task Backup(string backupFilePath, CancellationToken cancellationToken = default)
    {
        Calls.Add(nameof(Backup));
        BackupFilePathReceived = backupFilePath;
        BackupTokenReceived = cancellationToken;
        if (ThrowOnBackup)
            throw new InvalidOperationException("backup failed");
        return Task.CompletedTask;
    }

    public Task Restore(string backupFilePath, CancellationToken cancellationToken = default)
    {
        Calls.Add(nameof(Restore));
        RestoreFilePathReceived = backupFilePath;
        RestoreTokenReceived = cancellationToken;
        if (ThrowOnRestore)
            throw new InvalidOperationException("restore failed");
        return Task.CompletedTask;
    }
}
