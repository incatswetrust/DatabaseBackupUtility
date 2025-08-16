namespace DatabaseBackupUtility.Configs;

public class LocalStorageService : IStorageService
{
    public async Task SaveBackup(string sourceFilePath, string destinationPath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(sourceFilePath))
            {
                File.Copy(sourceFilePath, destinationPath, true);
                Console.WriteLine($"Backup saved locally at {destinationPath}");
            }
            else
            {
                Console.WriteLine($"Source file {sourceFilePath} does not exist.");
            }
        });
    }

    public async Task LoadBackup(string backupFilePath, string destinationPath)
    {
        await Task.Run(() =>
        {
            if (File.Exists(backupFilePath))
            {
                File.Copy(backupFilePath, destinationPath, true);
                Console.WriteLine($"Backup loaded from local storage to {destinationPath}");
            }
            else
            {
                Console.WriteLine($"Backup file {backupFilePath} does not exist.");
            }
        });
    }
}