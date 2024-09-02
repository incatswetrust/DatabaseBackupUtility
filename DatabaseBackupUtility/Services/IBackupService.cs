namespace DatabaseBackupUtility.Configs;

public interface IBackupService
{
    void CreateBackup(string backupFilePath);
}