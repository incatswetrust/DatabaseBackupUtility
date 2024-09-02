using Google.Cloud.Storage.V1;

namespace DatabaseBackupUtility.Configs;

public class GoogleCloudStorageService : IStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;

    public GoogleCloudStorageService(StorageClient storageClient, string bucketName)
    {
        _storageClient = storageClient;
        _bucketName = bucketName;
    }

    public void SaveBackup(string sourceFilePath, string destinationPath)
    {
        using var fileStream = File.OpenRead(sourceFilePath);
        try
        {
            _storageClient.UploadObject(_bucketName, destinationPath, null, fileStream);
            Console.WriteLine($"Backup uploaded to Google Cloud Storage bucket {_bucketName} at {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file to Google Cloud Storage: {ex.Message}");
        }
    }

    public void LoadBackup(string backupFilePath, string destinationPath)
    {
        using var outputFile = File.OpenWrite(destinationPath);
        try
        {
            _storageClient.DownloadObject(_bucketName, backupFilePath, outputFile);
            Console.WriteLine($"Backup downloaded from Google Cloud Storage bucket {_bucketName} to {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file from Google Cloud Storage: {ex.Message}");
        }
    }
}