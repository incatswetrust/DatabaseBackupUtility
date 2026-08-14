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

    if (isNotify && notificationConfig is not null)
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
                var typeOption = parser.GetOption("--type") ?? "full";
                if (!Enum.TryParse<BackupType>(typeOption, ignoreCase: true, out var backupType))
                {
                    Console.WriteLine($"Unknown backup type '{typeOption}'. Use full, incremental, or differential.");
                    return;
                }

                var compress = parser.HasFlag("--compress");
                var backupId = Guid.NewGuid().ToString("N");
                var workingBackupPath = Path.Combine(Path.GetTempPath(), $"backup_{backupId}.sql");
                var outputOption = parser.GetOption("--output");
                var backupFilePath = outputOption ?? Path.Combine(storageConfig.LocalPath,
                    $"backup_{dbConfig.DatabaseName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql{(compress ? ".gz" : string.Empty)}");

                var chainService = new BackupChainService(storageConfig.LocalPath);
                var existingManifests = await chainService.LoadManifestsAsync(dbConfig.DatabaseName, cancellationToken);
                var parent = BackupChainService.ResolveParent(existingManifests, backupType);
                var parentId = BackupChainService.ResolveImmediateParentId(existingManifests, backupType);

                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            var position = await backupService?.CreateBackup(backupId, workingBackupPath, backupType, parent, cancellationToken)!;
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

                            await chainService.SaveManifestAsync(new BackupManifest
                            {
                                Id = backupId,
                                DatabaseName = dbConfig.DatabaseName,
                                Type = backupType,
                                ParentId = parentId,
                                FullBackupId = backupType == BackupType.Full ? backupId : parent!.FullBackupId,
                                Position = position,
                                FileName = backupFilePath,
                                CreatedAtUtc = DateTime.UtcNow
                            }, cancellationToken);
                        },
                        logger!,
                        notificationService!,
                        $"Starting {typeOption} backup process...",
                        $"{typeOption} backup process completed successfully.",
                        $"{typeOption} backup process failed"
                    )
                );


                break;
            }
            case "restore":
            {
                var chainService = new BackupChainService(storageConfig.LocalPath);
                var manifests = await chainService.LoadManifestsAsync(dbConfig.DatabaseName, cancellationToken);
                var explicitFile = parser.GetOption("--file");
                var selectiveTarget = parser.GetOption("--table") ?? parser.GetOption("--collection");
                var targets = selectiveTarget is null ? null : new List<string> { selectiveTarget };

                var targetManifest = manifests.Count == 0
                    ? null
                    : string.IsNullOrEmpty(explicitFile)
                        ? BackupChainService.FindLatest(manifests)
                        : manifests.FirstOrDefault(m => m.FileName == explicitFile || Path.GetFileName(m.FileName) == explicitFile);

                if (targetManifest is not null)
                {
                    var chain = BackupChainService.ResolveChain(manifests, targetManifest.Id);
                    await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                            async () =>
                            {
                                var steps = new List<(string FilePath, BackupType Type)>();
                                foreach (var step in chain)
                                    steps.Add((await DownloadAndDecompressAsync(step.FileName), step.Type));

                                await restoreService?.RestoreChain(steps, targets, cancellationToken)!;
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

                // No chain metadata found (e.g. a backup taken before this version, or --file
                // naming a plain file): fall back to a single-file full restore.
                var backupFilePath = ResolveBackupFilePath(storageConfig.LocalPath, dbConfig.DatabaseName, explicitFile);
                if (backupFilePath is null)
                {
                    Console.WriteLine($"No backup file found for database '{dbConfig.DatabaseName}' in {storageConfig.LocalPath}. Use --file to specify one.");
                    return;
                }
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            var workingBackupPath = await DownloadAndDecompressAsync(backupFilePath);
                            await restoreService?.RestoreDatabase(workingBackupPath, BackupType.Full, targets, cancellationToken)!;
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

    async Task<string> DownloadAndDecompressAsync(string storedFilePath)
    {
        var isCompressed = storedFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        var downloadPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql{(isCompressed ? ".gz" : string.Empty)}");
        var workingBackupPath = isCompressed
            ? Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql")
            : downloadPath;

        await storageService!.LoadBackup(storedFilePath, downloadPath);
        if (isCompressed)
        {
            await CompressionService.DecompressFileAsync(downloadPath, workingBackupPath);
            File.Delete(downloadPath);
        }

        return workingBackupPath;
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
