namespace DatabaseBackupUtility.Configs;

public class LocalStorageService : IStorageService
{
    public void SaveBackup(string sourceFilePath, string destinationPath)
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
    }

    public void LoadBackup(string backupFilePath, string destinationPath)
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
    }
}