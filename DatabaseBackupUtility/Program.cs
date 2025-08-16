using DatabaseBackupUtility.Configs;
using DatabaseBackupUtility.Models;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Serilog;


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

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console()
        .CreateLogger();

    await using var serviceProvider = new ServiceCollection()
        .AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>()
        .AddSingleton(sp => sp.GetService<IDatabaseConnectionFactory>()!.CreateConnection(dbConfig))
        .AddSingleton<IConfiguration>(configuration)
        .AddSingleton<ILoggingService, SerilogLoggingService>()
        .AddSingleton<INotificationService>(_ => new SlackNotificationService(notificationConfig.SlackWebhookUrl))
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
                var backupFilePath = Path.Combine(storageConfig.LocalPath, "backup.sql");
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            await backupService?.CreateBackup(backupFilePath)!;
                            await storageService?.SaveBackup(backupFilePath, backupFilePath)!;
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
                var backupFilePath = Path.Combine(storageConfig.LocalPath, "backup.sql");
                await retryPolicy.ExecuteAsync(() => ProcessWithLoggingAsync(
                        async () =>
                        {
                            await storageService?.LoadBackup(backupFilePath, backupFilePath)!;
                            await restoreService?.RestoreDatabase(backupFilePath)!;
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

    void ShowErrors(List<ValidationFailure> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error.ErrorMessage);
        }
    }
