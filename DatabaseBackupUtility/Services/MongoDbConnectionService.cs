using System.Diagnostics;
using MongoDB.Driver;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

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

    public async Task<bool> TestConnection()
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

    public Task Connect()
    {
        // MongoDB automatically manages the connection
        Console.WriteLine("Connected to MongoDB database.");
        return Task.CompletedTask;
    }

    public Task Disconnect()
    {
        // MongoDB driver does not require explicit connection closing
        Console.WriteLine("Disconnected from MongoDB database.");
        return Task.CompletedTask;
    }

    public async Task Backup(string backupFilePath, CancellationToken cancellationToken = default)
    {
        // Using the `mongodump` utility
        var backupCommand =
            $"mongodump --uri=\"{_connectionString}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" --out=\"{backupFilePath}\"";
        await ExecuteCommand(backupCommand, cancellationToken);
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    public async Task Restore(string backupFilePath, CancellationToken cancellationToken = default)
    {
        // Using the `mongorestore` utility
        var restoreCommand =
            $"mongorestore --uri=\"{_connectionString}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" \"{backupFilePath}\"";
        await ExecuteCommand(restoreCommand, cancellationToken);
        Console.WriteLine($"Database restored from {backupFilePath}");
    }

    private static async Task ExecuteCommand(string command, CancellationToken cancellationToken)
    {
        var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processInfo)!;
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Command failed (exit {process.ExitCode}): {error}");
        }
    }
}