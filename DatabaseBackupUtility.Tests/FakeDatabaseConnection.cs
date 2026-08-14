using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Tests;

internal class FakeDatabaseConnection : IDatabaseConnection
{
    public List<string> Calls { get; } = [];
    public string? BackupIdReceived { get; private set; }
    public string? BackupFilePathReceived { get; private set; }
    public string? RestoreFilePathReceived { get; private set; }
    public BackupType BackupTypeReceived { get; private set; }
    public BackupType RestoreTypeReceived { get; private set; }
    public IReadOnlyList<string>? RestoreTargetsReceived { get; private set; }
    public BackupParent? ParentReceived { get; private set; }
    public CancellationToken BackupTokenReceived { get; private set; }
    public CancellationToken RestoreTokenReceived { get; private set; }
    public bool ThrowOnBackup { get; set; }
    public bool ThrowOnRestore { get; set; }
    public bool TestConnectionResult { get; set; } = true;
    public string? PositionToReturn { get; set; }

    // Invoked synchronously inside Restore(), before returning, so a test can inspect
    // filesystem state (e.g. a merged temp directory) that gets cleaned up right after.
    public Action<string>? OnRestore { get; set; }

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

    public Task<string?> Backup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default)
    {
        Calls.Add(nameof(Backup));
        BackupIdReceived = backupId;
        BackupFilePathReceived = backupFilePath;
        BackupTypeReceived = type;
        ParentReceived = parent;
        BackupTokenReceived = cancellationToken;
        if (ThrowOnBackup)
            throw new InvalidOperationException("backup failed");
        return Task.FromResult(PositionToReturn);
    }

    public Task Restore(string backupFilePath, BackupType type = BackupType.Full, IReadOnlyList<string>? targets = null,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(nameof(Restore));
        RestoreFilePathReceived = backupFilePath;
        RestoreTypeReceived = type;
        RestoreTargetsReceived = targets;
        RestoreTokenReceived = cancellationToken;
        OnRestore?.Invoke(backupFilePath);
        if (ThrowOnRestore)
            throw new InvalidOperationException("restore failed");
        return Task.CompletedTask;
    }
}
