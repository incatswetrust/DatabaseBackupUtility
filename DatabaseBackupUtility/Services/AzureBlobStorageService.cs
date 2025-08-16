using Azure.Storage.Blobs;

namespace DatabaseBackupUtility.Configs;

public class AzureBlobStorageService(BlobServiceClient blobServiceClient, string containerName) : IStorageService
{
    public void SaveBackup(string sourceFilePath, string destinationPath)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(destinationPath);
        try
        {
            using var uploadFileStream = File.OpenRead(sourceFilePath);
            blobClient.Upload(uploadFileStream, true);
            Console.WriteLine($"Backup uploaded to Azure Blob Storage container {containerName} at {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file to Azure Blob Storage: {ex.Message}");
        }
    }

    public void LoadBackup(string backupFilePath, string destinationPath)
    {
        var blobClient = blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(backupFilePath);
        try
        {
            blobClient.DownloadTo(destinationPath);
            Console.WriteLine($"Backup downloaded from Azure Blob Storage container {containerName} to {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file from Azure Blob Storage: {ex.Message}");
        }
    }
}