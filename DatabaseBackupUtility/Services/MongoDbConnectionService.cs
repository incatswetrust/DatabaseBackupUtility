using System.Diagnostics;
using MongoDB.Driver;
namespace DatabaseBackupUtility.Configs;

public class MongoDbConnectionService : IDatabaseConnection
{
    private readonly string _connectionString;
    private readonly MongoClient _client;
    private readonly IMongoDatabase _database;

    public MongoDbConnectionService(string connectionString, string databaseName)
    {
        _connectionString = connectionString;
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
        var backupCommand =
            $"mongodump --uri=\"{_connectionString}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" --out=\"{backupFilePath}\"";
        await ExecuteCommand(backupCommand);
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    public async Task Restore(string backupFilePath)
    {
        // Using the `mongorestore` utility
        var restoreCommand =
            $"mongorestore --uri=\"{_connectionString}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" \"{backupFilePath}\"";
        await ExecuteCommand(restoreCommand);
        Console.WriteLine($"Database restored from {backupFilePath}");
    }

    private static async Task ExecuteCommand(string command)
    {
        var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processInfo)!;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Command failed (exit {process.ExitCode}): {error}");
        }
    }
}