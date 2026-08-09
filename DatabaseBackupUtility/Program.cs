using DatabaseBackupUtility.Factories;
using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services;
using DatabaseBackupUtility.Services.Interfaces;
using DatabaseBackupUtility.Validators;
using FluentValidation.Results;
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
        .AddBackupAndRestoreServices(dbConfig)
        .AddStorageService(storageConfig)
        .BuildServiceProvider();

    var logger = serviceProvider.GetService<ILoggingService>();
    var notificationService = serviceProvider.GetService<INotificationService>();
    var backupService = serviceProvider.GetService<IBackupService>();
    var restoreService = serviceProvider.GetService<IRestoreService>();
    var storageService = serviceProvider.GetService<IStorageService>();
    var dbConnection = serviceProvider.GetService<IDatabaseConnection>();

    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
    logger?.LogInfo($"DatabaseBackupUtility v{version} starting. Args: {string.Join(' ', args)}");

    if (parser.HasFlag("--dry-run"))
    {
        Console.WriteLine("Dry run: configuration is valid.");
        var canConnect = await dbConnection!.TestConnection();
        Console.WriteLine(canConnect
            ? "Dry run: connection to the database succeeded."
            : "Dry run: connection to the database failed.");
        return;
    }

    using var cancellationTokenSource = new CancellationTokenSource();
    Console.CancelKeyPress += (_, cancelEventArgs) =>
    {
        cancelEventArgs.Cancel = true;
        Console.WriteLine("Cancellation requested, stopping...");
        cancellationTokenSource.Cancel();
    };
    var cancellationToken = cancellationTokenSource.Token;

    try
    {
        var retryPolicy = Policy
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        switch (command)
        {
            case "backup":
            {
                var compress = parser.HasFlag("--compress");
                var workingBackupPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql");
                var outputOption = parser.GetOption("--output");
                var backupFilePath = outputOption ?? Path.Combine(storageConfig.LocalPath,
                    $"backup_{dbConfig.DatabaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql{(compress ? ".gz" : string.Empty)}");
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            await backupService?.CreateBackup(workingBackupPath, cancellationToken)!;
                            var uploadPath = workingBackupPath;
                            if (compress)
                            {
                                uploadPath = workingBackupPath + ".gz";
                                await CompressionService.CompressFileAsync(workingBackupPath, uploadPath);
                                File.Delete(workingBackupPath);
                            }
                            await storageService?.SaveBackup(uploadPath, backupFilePath)!;
                            if (compress)
                                File.Delete(uploadPath);
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
                var isCompressed = backupFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
                var downloadPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql{(isCompressed ? ".gz" : string.Empty)}");
                var workingBackupPath = isCompressed
                    ? Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql")
                    : downloadPath;
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            await storageService?.LoadBackup(backupFilePath, downloadPath)!;
                            if (isCompressed)
                            {
                                await CompressionService.DecompressFileAsync(downloadPath, workingBackupPath);
                                File.Delete(downloadPath);
                            }
                            await restoreService?.RestoreDatabase(workingBackupPath, cancellationToken)!;
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
            case "test-connection":
            {
                var canConnect = await dbConnection!.TestConnection();
                Console.WriteLine(canConnect
                    ? "Connection to the database succeeded."
                    : "Connection to the database failed.");
                break;
            }
            case "list":
            {
                if (storageConfig.Type != "Local")
                {
                    Console.WriteLine($"Listing backups is only supported for Local storage (current type: {storageConfig.Type}).");
                    break;
                }

                var backups = Directory.Exists(storageConfig.LocalPath)
                    ? Directory.GetFiles(storageConfig.LocalPath, $"backup_{dbConfig.DatabaseName}_*.sql*")
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .ToList()
                    : [];

                if (backups.Count == 0)
                {
                    Console.WriteLine($"No backups found for database '{dbConfig.DatabaseName}' in {storageConfig.LocalPath}.");
                    break;
                }

                Console.WriteLine($"Backups for database '{dbConfig.DatabaseName}' in {storageConfig.LocalPath}:");
                foreach (var backup in backups)
                {
                    var info = new FileInfo(backup);
                    Console.WriteLine($"  {Path.GetFileName(backup)}  ({info.Length} bytes, {info.LastWriteTimeUtc:u})");
                }
                break;
            }
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Operation was cancelled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during backup process: {ex.Message}");
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
            await notification.SendNotification(successMessage);
        }
        catch (Exception ex)
        {
            log.LogError($"{errorMessage}: {ex.Message}");
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
