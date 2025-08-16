namespace DatabaseBackupUtility.Configs;

public interface IRestoreService
{
    Task RestoreDatabase(string backupFilePath);
}