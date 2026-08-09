using Amazon.S3;
using Azure.Storage.Blobs;
using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.DependencyInjection;

namespace DatabaseBackupUtility.Services;

public static class ServiceRegistrar
{
    public static IServiceCollection AddBackupAndRestoreServices(this IServiceCollection services, DatabaseConfig dbConfig)
    {
        return services
            .AddSingleton<IBackupService>(sp => dbConfig.Type switch
            {
                "MySql" => new MySqlBackupService(sp.GetRequiredService<IDatabaseConnection>()),
                "PostgreSql" => new PostgreSqlBackupService(sp.GetRequiredService<IDatabaseConnection>()),
                "MongoDb" => new MongoDbBackupService(sp.GetRequiredService<IDatabaseConnection>()),
                _ => throw new InvalidOperationException($"Unsupported database type: {dbConfig.Type}")
            })
            .AddSingleton<IRestoreService>(sp => dbConfig.Type switch
            {
                "MySql" => new MySqlRestoreService(sp.GetRequiredService<IDatabaseConnection>()),
                "PostgreSql" => new PostgreSqlRestoreService(sp.GetRequiredService<IDatabaseConnection>()),
                "MongoDb" => new MongoDbRestoreService(sp.GetRequiredService<IDatabaseConnection>()),
                _ => throw new InvalidOperationException($"Unsupported database type: {dbConfig.Type}")
            });
    }

    public static IServiceCollection AddStorageService(this IServiceCollection services, Storage storageConfig)
    {
        return services.AddSingleton<IStorageService>(_ => storageConfig.Type switch
        {
            "Local" => new LocalStorageService(),
            "S3" => new AwsS3StorageService(new AmazonS3Client(), storageConfig.Cloud.BucketName),
            "Azure" => new AzureBlobStorageService(
                new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")),
                storageConfig.Cloud.BucketName),
            "Google" => new GoogleCloudStorageService(StorageClient.Create(), storageConfig.Cloud.BucketName),
            _ => throw new InvalidOperationException($"Unsupported storage type: {storageConfig.Type}")
        });
    }
}
