namespace DatabaseBackupUtility.Configs;

public interface IBackupService
{
    Task CreateBackup(string backupFilePath);
}