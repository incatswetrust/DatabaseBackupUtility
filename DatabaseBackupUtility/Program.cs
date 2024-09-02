using DatabaseBackupUtility.Configs;
using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Serilog;

var parser = new CommandLineParser(args);

        if (!parser.IsValid())
        {
            return;
        }

        string command = parser.GetCommand();
        string configPath = parser.GetOption("--config");

        if (string.IsNullOrEmpty(configPath))
        {
            Console.WriteLine("Configuration file path is required.");
            parser.ShowUsage();
            return;
        }

        // Настройка конфигурации
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .Build();

        // Настройка Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .CreateLogger();

        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration) // Добавляем IConfiguration в DI контейнер
            .AddSingleton<ILoggingService, SerilogLoggingService>()
            .AddSingleton<INotificationService>(sp =>
            {
                var configSection = configuration.GetSection("Notifications"); // Получаем секцию "Notifications"
                var webhookUrl = configSection.GetValue<string>("SlackWebhookUrl"); // Извлекаем значение SlackWebhookUrl

                return new SlackNotificationService(webhookUrl);
            })
            .AddSingleton<IDatabaseConnection>(sp =>
            {
                var dbConfigSection = configuration.GetSection("Database"); // Получаем секцию "Database"
                var dbType = dbConfigSection.GetValue<string>("Type"); // Извлекаем значение типа базы данных

                if (dbType == "MySql")
                {
                    return new MySqlConnectionService(
                        dbConfigSection.GetValue<string>("Host"),
                        dbConfigSection.GetValue<string>("DatabaseName"),
                        dbConfigSection.GetValue<string>("Username"),
                        dbConfigSection.GetValue<string>("Password")
                    );
                }
                else if (dbType == "PostgreSql")
                {
                    return new PostgreSqlConnectionService(
                        dbConfigSection.GetValue<string>("Host"),
                        dbConfigSection.GetValue<string>("DatabaseName"),
                        dbConfigSection.GetValue<string>("Username"),
                        dbConfigSection.GetValue<string>("Password")
                    );
                }
                else if (dbType == "MongoDb")
                {
                    return new MongoDbConnectionService(
                        $"mongodb://{dbConfigSection.GetValue<string>("Username")}:{dbConfigSection.GetValue<string>("Password")}@{dbConfigSection.GetValue<string>("Host")}",
                        dbConfigSection.GetValue<string>("DatabaseName")
                    );
                }
                else
                {
                    throw new Exception("Unsupported database type.");
                }
            })
            .BuildServiceProvider();

        var logger = serviceProvider.GetService<ILoggingService>();
        var notificationService = serviceProvider.GetService<INotificationService>();
        var backupService = serviceProvider.GetService<IBackupService>();
        var restoreService = serviceProvider.GetService<IRestoreService>();
        var storageService = serviceProvider.GetService<IStorageService>();

        try
        {
            if (command == "backup")
            {
                logger.LogInfo("Starting backup process...");
                var localPath =
                    configuration.GetValue<string>("Storage:LocalPath"); // Получаем путь для хранения резервной копии
                string backupFilePath = Path.Combine(localPath, "backup.sql");
                backupService.CreateBackup(backupFilePath);
                storageService.SaveBackup(backupFilePath, backupFilePath);
                logger.LogInfo("Backup process completed successfully.");
                notificationService.SendNotification("Backup process completed successfully.");
            }
            else if (command == "restore")
            {
                logger.LogInfo("Starting restore process...");
                var localPath =
                    configuration.GetValue<string>("Storage:LocalPath"); // Получаем путь для восстановления резервной копии
                string backupFilePath = Path.Combine(localPath, "backup.sql");
                storageService.LoadBackup(backupFilePath, backupFilePath);
                restoreService.RestoreDatabase(backupFilePath);
                logger.LogInfo("Restore process completed successfully.");
                notificationService.SendNotification("Restore process completed successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred: {ex.Message}");
            notificationService.SendNotification($"Process failed: {ex.Message}");
        } 