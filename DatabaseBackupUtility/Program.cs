using DatabaseBackupUtility.Configs;
using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Polly;
        using Serilog;

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
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .CreateLogger();

        await using var serviceProvider = new ServiceCollection()
                .AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>()
                .AddSingleton(sp =>
                {
                    var dbConfigSection = sp.GetService<IConfiguration>()!.GetSection("Database");
                    return sp.GetService<IDatabaseConnectionFactory>()!.CreateConnection(dbConfigSection);
                })

            .AddSingleton<IConfiguration>(configuration) 
            .AddSingleton<ILoggingService, SerilogLoggingService>()
            .AddSingleton<INotificationService>(sp =>
            {
                var configSection = configuration.GetSection("Notifications"); 
                var webhookUrl = configSection.GetValue<string>("SlackWebhookUrl");

                return new SlackNotificationService(webhookUrl);
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
                    var localPath =
                        configuration.GetValue<string>("Storage:LocalPath"); 
                    if (localPath != null)
                    {
                        var backupFilePath = Path.Combine(localPath, "backup.sql");
                        await retryPolicy.ExecuteAsync(() =>  ProcessWithLoggingAsync(
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
                    }
                    break;
                }
                case "restore":
                {
                    var localPath =
                        configuration.GetValue<string>("Storage:LocalPath"); 
                    if (localPath != null)
                    {
                        var backupFilePath = Path.Combine(localPath, "backup.sql");
                        await retryPolicy.ExecuteAsync(() =>  ProcessWithLoggingAsync(
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
                    }
                    break;
                }
            }
        }
        catch 
        {
            
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
