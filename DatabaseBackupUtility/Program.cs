using DatabaseBackupUtility.Configs;
using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
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

        var serviceProvider = new ServiceCollection()
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
            switch (command)
            {
                case "backup":
                {
                    logger?.LogInfo("Starting backup process...");
                    var localPath =
                        configuration.GetValue<string>("Storage:LocalPath"); // Получаем путь для хранения резервной копии
                    if (localPath != null)
                    {
                        var backupFilePath = Path.Combine(localPath, "backup.sql");
                        backupService?.CreateBackup(backupFilePath);
                        storageService?.SaveBackup(backupFilePath, backupFilePath);
                    }

                    logger?.LogInfo("Backup process completed successfully.");
                    notificationService?.SendNotification("Backup process completed successfully.");
                    break;
                }
                case "restore":
                {
                    logger?.LogInfo("Starting restore process...");
                    var localPath =
                        configuration.GetValue<string>("Storage:LocalPath"); // Получаем путь для восстановления резервной копии
                    if (localPath != null)
                    {
                        var backupFilePath = Path.Combine(localPath, "backup.sql");
                        storageService?.LoadBackup(backupFilePath, backupFilePath);
                        restoreService?.RestoreDatabase(backupFilePath);
                    }

                    logger?.LogInfo("Restore process completed successfully.");
                    notificationService?.SendNotification("Restore process completed successfully.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError($"An error occurred: {ex.Message}");
            notificationService?.SendNotification($"Process failed: {ex.Message}");
        } 