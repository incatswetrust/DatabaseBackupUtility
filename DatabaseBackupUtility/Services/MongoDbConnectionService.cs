using MongoDB.Driver;
namespace DatabaseBackupUtility.Configs;

public class MongoDbConnectionService : IDatabaseConnection
{
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;

    public MongoDbConnectionService(string connectionString, string databaseName)
    {
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);
    }

    public async Task <bool> TestConnection()
    {
        try
        {
            await _client.ListDatabaseNamesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task Connect()
    {
        // MongoDB automatically manages the connection
        await Task.Delay(1);
        Console.WriteLine("Connected to MongoDB database.");
    }

    public async Task Disconnect()
    {
        // MongoDB driver does not require explicit connection closing
        await Task.Delay(1);
        Console.WriteLine("Disconnected from MongoDB database.");
    }

    public async Task Backup(string backupFilePath)
    {
        // Using the `mongodump` utility
        await Task.Run(() =>
        {
            var backupCommand =
                $"mongodump --uri=\"{_client.Settings.Server}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" --out=\"{backupFilePath}\"";
            System.Diagnostics.Process.Start("bash", $"-c \"{backupCommand}\"");
            Console.WriteLine($"Backup created at {backupFilePath}");
        });
    }

    public async Task Restore(string backupFilePath)
    {
        // Using the `mongorestore` utility
        await Task.Run(() =>
        {
            var restoreCommand =
                $"mongorestore --uri=\"{_client.Settings.Server}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" \"{backupFilePath}\"";
            System.Diagnostics.Process.Start("bash", $"-c \"{restoreCommand}\"");
            Console.WriteLine($"Database restored from {backupFilePath}");
        });
    }
}