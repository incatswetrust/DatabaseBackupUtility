namespace DatabaseBackupUtility.Services.Interfaces;

public interface IRestoreService
{
    Task RestoreDatabase(string backupFilePath, CancellationToken cancellationToken = default);
}