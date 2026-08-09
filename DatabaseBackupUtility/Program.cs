using Amazon.S3;
using Azure.Storage.Blobs;
using DatabaseBackupUtility.Configs;
using DatabaseBackupUtility.Models;
using FluentValidation.Results;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;


    var isNotify = true;
    var parser = new CommandLineParser(args);

    if (!parser.IsValid())
        return;
    var command = parser.GetCommand();
    var configPath = parser.GetOption("--config");

    if (string.IsNullOrEmpty(configPath))
    {
        Console.WriteLine("Configuration file path is required.");
        CommandLineParser.ShowUsage();
        return;
    }

    if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), configPath)))
    {
        Console.WriteLine($"Configuration file not found: {configPath}");
        return;
    }

    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile(configPath, optional: false, reloadOnChange: true)
        .Build();

    var dbConfig = configuration.GetSection("Database").Get<DatabaseConfig>();
    var dbValidator = new DatabaseConfigValidator();
    if (dbConfig is null)
    {
        Console.WriteLine("Database configuration are required");
        return;
    }
    var validationResult = dbValidator.Validate(dbConfig);
    if (!validationResult.IsValid)
    {
        ShowErrors(validationResult.Errors);
        return;
    }
    var notificationConfig = configuration.GetSection("Notifications").Get<Notifications>();
    if (notificationConfig is null)
        isNotify = false;

    if (isNotify)
    {
        var notificationValidator = new NotificationConfigValidator();
        validationResult = notificationValidator.Validate(notificationConfig);
        if (!validationResult.IsValid)
        {
            ShowErrors(validationResult.Errors);
            return;
        }
    }

    var storageConfig =
        configuration.GetSection("Storage").Get<Storage>();
    if (storageConfig is null)
    {
        Console.WriteLine("Storage configuration are required");
        return;
    }

    var storageValidator = new StorageConfigValidator();
    validationResult = storageValidator.Validate(storageConfig);
    if (!validationResult.IsValid)
    {
        ShowErrors(validationResult.Errors);
        return;
    }

    if (storageConfig.Type == "Local" && !Directory.Exists(storageConfig.LocalPath))
    {
        Directory.CreateDirectory(storageConfig.LocalPath);
    }

    var loggingSettings = configuration.GetSection("Logging").Get<LoggingSettings>() ?? new LoggingSettings();

    await using var serviceProvider = new ServiceCollection()
        .AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>()
        .AddSingleton(sp => sp.GetService<IDatabaseConnectionFactory>()!.CreateConnection(dbConfig))
        .AddSingleton<IConfiguration>(configuration)
        .AddSingleton<ILoggingService>(_ => new SerilogLoggingService(loggingSettings))
        .AddSingleton<INotificationService>(_ => isNotify
            ? new SlackNotificationService(notificationConfig!.SlackWebhookUrl)
            : new NullNotificationService())
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
        })
        .AddSingleton<IStorageService>(_ => storageConfig.Type switch
        {
            "Local" => new LocalStorageService(),
            "S3" => new AwsS3StorageService(new AmazonS3Client(), storageConfig.Cloud.BucketName),
            "Azure" => new AzureBlobStorageService(
                new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")),
                storageConfig.Cloud.BucketName),
            "Google" => new GoogleCloudStorageService(StorageClient.Create(), storageConfig.Cloud.BucketName),
            _ => throw new InvalidOperationException($"Unsupported storage type: {storageConfig.Type}")
        })
        .BuildServiceProvider();

    var logger = serviceProvider.GetService<ILoggingService>();
    var notificationService = serviceProvider.GetService<INotificationService>();
    var backupService = serviceProvider.GetService<IBackupService>();
    var restoreService = serviceProvider.GetService<IRestoreService>();
    var storageService = serviceProvider.GetService<IStorageService>();

    try
    {
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        switch (command)
        {
            case "backup":
            {
                var workingBackupPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql");
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var backupFilePath = Path.Combine(storageConfig.LocalPath,
                    $"backup_{dbConfig.DatabaseName}_{timestamp}.sql");
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            await backupService?.CreateBackup(workingBackupPath)!;
                            await storageService?.SaveBackup(workingBackupPath, backupFilePath)!;
                        },
                        logger!,
                        notificationService!,
                        "Starting backup process...",
                        "Backup process completed successfully.",
                        "Backup process failed"
                    )
                );


                break;
            }
            case "restore":
            {
                var backupFilePath = ResolveBackupFilePath(storageConfig.LocalPath, dbConfig.DatabaseName,
                    parser.GetOption("--file"));
                if (backupFilePath is null)
                {
                    Console.WriteLine($"No backup file found for database '{dbConfig.DatabaseName}' in {storageConfig.LocalPath}. Use --file to specify one.");
                    return;
                }
                var workingBackupPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql");
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            await storageService?.LoadBackup(backupFilePath, workingBackupPath)!;
                            await restoreService?.RestoreDatabase(workingBackupPath)!;
                        },
                        logger!,
                        notificationService!,
                        "Starting restore process...",
                        "Restore process completed successfully.",
                        "Restore process failed"
                    )
                );


                break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during backup process: {ex.Message}");
        if(isNotify)
            await notificationService?.SendNotification($"Error during backup process: {ex.Message}")!;
    }

    return;


    async Task ProcessWithLoggingAsync(
        Func<Task> action,
        ILoggingService log,
        INotificationService notification,
        string startMessage,
        string successMessage,
        string errorMessage)
    {
        try
        {
            log.LogInfo(startMessage);
            await action();
            log.LogInfo(successMessage);
            if(isNotify)
                await notification.SendNotification(successMessage);
        }
        catch (Exception ex)
        {
            log.LogError($"{errorMessage}: {ex.Message}");
            if(isNotify)
                await notification.SendNotification($"{errorMessage}: {ex.Message}");
            throw;
        }
    }

    string? ResolveBackupFilePath(string localPath, string databaseName, string? explicitFile)
    {
        if (!string.IsNullOrEmpty(explicitFile))
            return Path.Combine(localPath, explicitFile);

        if (!Directory.Exists(localPath))
            return null;

        return Directory.GetFiles(localPath, $"backup_{databaseName}_*.sql*")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    void ShowErrors(List<ValidationFailure> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error.ErrorMessage);
        }
    }
