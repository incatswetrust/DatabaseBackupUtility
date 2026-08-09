namespace DatabaseBackupUtility.Services.Interfaces;

public interface IStorageService
{
    Task SaveBackup(string sourceFilePath, string destinationPath);
    Task LoadBackup(string backupFilePath, string destinationPath);
}