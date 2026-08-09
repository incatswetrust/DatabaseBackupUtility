namespace DatabaseBackupUtility.Services.Interfaces;

public interface IBackupService
{
    Task CreateBackup(string backupFilePath, CancellationToken cancellationToken = default);
}