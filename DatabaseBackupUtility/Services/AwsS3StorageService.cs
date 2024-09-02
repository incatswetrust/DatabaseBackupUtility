using Amazon.S3;
using Amazon.S3.Transfer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseBackupUtility.Configs;


public class AwsS3StorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public AwsS3StorageService(IAmazonS3 s3Client, string bucketName)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
    }

    public void SaveBackup(string sourceFilePath, string destinationPath)
    {
        var fileTransferUtility = new TransferUtility(_s3Client);
        try
        {
            fileTransferUtility.Upload(sourceFilePath, _bucketName, destinationPath);
            Console.WriteLine($"Backup uploaded to S3 bucket {_bucketName} at {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file to S3: {ex.Message}");
        }
    }

    public void LoadBackup(string backupFilePath, string destinationPath)
    {
        var fileTransferUtility = new TransferUtility(_s3Client);
        try
        {
            fileTransferUtility.Download(destinationPath, _bucketName, backupFilePath);
            Console.WriteLine($"Backup downloaded from S3 bucket {_bucketName} to {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file from S3: {ex.Message}");
        }
    }
}
