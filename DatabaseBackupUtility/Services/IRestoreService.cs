namespace DatabaseBackupUtility.Configs;

public interface IRestoreService
{
    void RestoreDatabase(string backupFilePath);
}