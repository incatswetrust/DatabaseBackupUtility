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

    public bool TestConnection()
    {
        try
        {
            _client.ListDatabaseNames();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Connect()
    {
        // MongoDB automatically manages the connection
        Console.WriteLine("Connected to MongoDB database.");
    }

    public void Disconnect()
    {
        // MongoDB driver does not require explicit connection closing
        Console.WriteLine("Disconnected from MongoDB database.");
    }

    public void Backup(string backupFilePath)
    {
        // Using the `mongodump` utility
        var backupCommand = $"mongodump --uri=\"{_client.Settings.Server}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" --out=\"{backupFilePath}\"";
        System.Diagnostics.Process.Start("bash", $"-c \"{backupCommand}\"");
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    public void Restore(string backupFilePath)
    {
        // Using the `mongorestore` utility
        var restoreCommand = $"mongorestore --uri=\"{_client.Settings.Server}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" \"{backupFilePath}\"";
        System.Diagnostics.Process.Start("bash", $"-c \"{restoreCommand}\"");
        Console.WriteLine($"Database restored from {backupFilePath}");
    }
}