using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseBackupUtility.Configs;


public class AwsS3StorageService(IAmazonS3 s3Client, string bucketName) : IStorageService
{
    public async Task SaveBackup(string sourceFilePath, string destinationPath)
    {
        var fileTransferUtility = new TransferUtility(s3Client);
        try
        {
            await fileTransferUtility.UploadAsync(sourceFilePath, bucketName, destinationPath);
            Console.WriteLine($"Backup uploaded to S3 bucket {bucketName} at {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file to S3: {ex.Message}");
            throw;
        }
    }

    public async Task LoadBackup(string backupFilePath, string destinationPath)
    {
        var fileTransferUtility = new TransferUtility(s3Client);
        try
        {
            await fileTransferUtility.DownloadAsync(destinationPath, bucketName, backupFilePath);
            Console.WriteLine($"Backup downloaded from S3 bucket {bucketName} to {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file from S3: {ex.Message}");
            throw;
        }
    }
}
