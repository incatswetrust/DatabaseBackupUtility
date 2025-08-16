using Google.Cloud.Storage.V1;

namespace DatabaseBackupUtility.Configs;

public class GoogleCloudStorageService(StorageClient storageClient, string bucketName) : IStorageService
{
    public async Task SaveBackup(string sourceFilePath, string destinationPath)
    {
        await using var fileStream = File.OpenRead(sourceFilePath);
        try
        {
            storageClient.UploadObject(bucketName, destinationPath, null, fileStream);
            Console.WriteLine($"Backup uploaded to Google Cloud Storage bucket {bucketName} at {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file to Google Cloud Storage: {ex.Message}");
        }
    }

    public async Task LoadBackup(string backupFilePath, string destinationPath)
    {
        await using var outputFile = File.OpenWrite(destinationPath);
        try
        {
            storageClient.DownloadObject(bucketName, backupFilePath, outputFile);
            Console.WriteLine($"Backup downloaded from Google Cloud Storage bucket {bucketName} to {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file from Google Cloud Storage: {ex.Message}");
        }
    }
}