using MongoDB.Bson;
using MongoDB.Driver;
using DatabaseBackupUtility.Models;
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

    public async Task<string?> Backup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default)
    {
        if (type == BackupType.Full)
            return await FullBackup(backupFilePath, cancellationToken);

        return await IncrementalBackup(backupFilePath, parent?.Position, cancellationToken);
    }

    private async Task<string?> FullBackup(string backupFilePath, CancellationToken cancellationToken)
    {
        // Using the `mongodump` utility
        var backupCommand =
            $"mongodump --uri=\"{_connectionString}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" --out=\"{backupFilePath}\"";
        await ProcessRunner.RunAsync("mongodump", backupCommand, cancellationToken);
        Console.WriteLine($"Backup created at {backupFilePath}");
        return await GetLatestOplogTimestampAsync(cancellationToken);
    }

    // Requires the source to be a replica set (a standalone mongod has no oplog). Dumps the
    // `local.oplog.rs` entries written since the parent's timestamp, then relocates the dump to
    // where mongorestore --oplogReplay expects to find it: an `oplog.bson` file at the root of the
    // restore directory.
    private async Task<string?> IncrementalBackup(string backupFilePath, string? sincePosition, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sincePosition))
            throw new InvalidOperationException("Cannot create an incremental/differential MongoDB backup: the parent backup has no recorded oplog timestamp.");

        var (seconds, ordinal) = ParseTimestamp(sincePosition);
        var query = $"{{\"ts\": {{\"$gt\": Timestamp({seconds}, {ordinal})}}}}";
        var backupCommand =
            $"mongodump --uri=\"{_connectionString}\" --db=local --collection=oplog.rs --query='{query}' --out=\"{backupFilePath}\"";
        await ProcessRunner.RunAsync("mongodump", backupCommand, cancellationToken);

        var dumpedOplogFile = Path.Combine(backupFilePath, "local", "oplog.rs.bson");
        var oplogFile = Path.Combine(backupFilePath, "oplog.bson");
        if (File.Exists(dumpedOplogFile))
            File.Move(dumpedOplogFile, oplogFile, overwrite: true);

        Console.WriteLine($"Backup created at {backupFilePath}");
        return await GetLatestOplogTimestampAsync(cancellationToken);
    }

    private async Task<string?> GetLatestOplogTimestampAsync(CancellationToken cancellationToken)
    {
        var oplog = _client.GetDatabase("local").GetCollection<BsonDocument>("oplog.rs");
        var latest = await oplog.Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("$natural"))
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null || !latest.TryGetValue("ts", out var tsValue))
            return null;

        var ts = tsValue.AsBsonTimestamp;
        return $"{ts.Timestamp}:{ts.Increment}";
    }

    private static (int seconds, int ordinal) ParseTimestamp(string position)
    {
        var parts = position.Split(':', 2);
        return (int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public async Task Restore(string backupFilePath, BackupType type = BackupType.Full, CancellationToken cancellationToken = default)
    {
        var restoreCommand = type == BackupType.Full
            ? $"mongorestore --uri=\"{_connectionString}\" --db=\"{_database.DatabaseNamespace.DatabaseName}\" \"{backupFilePath}\""
            : $"mongorestore --uri=\"{_connectionString}\" --oplogReplay --dir=\"{backupFilePath}\"";
        await ProcessRunner.RunAsync("mongorestore", restoreCommand, cancellationToken);
        Console.WriteLine($"Database restored from {backupFilePath}");
    }
}
