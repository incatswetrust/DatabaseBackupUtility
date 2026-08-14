using Npgsql;
using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class PostgreSqlConnectionService : IDatabaseConnection
{
    private readonly string _connectionString;
    private readonly string _host;
    private readonly int? _port;
    private readonly string _database;
    private readonly string _username;
    private readonly string _password;
    private NpgsqlConnection? _connection;

    public PostgreSqlConnectionService(string host, int? port, string database, string username, string password)
    {
        _host = host;
        _port = port;
        _database = database;
        _username = username;
        _password = password;
        var portSegment = port.HasValue ? $"Port={port};" : string.Empty;
        _connectionString = $"Host={host};{portSegment}Database={database};Username={username};Password={password};";
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            await Connect();
            await Disconnect();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task Connect()
    {
        try
        {
            _connection = new NpgsqlConnection(_connectionString);
            await _connection.OpenAsync();
            Console.WriteLine("Connected to PostgreSQL database.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.DescribeConnectionFailure("PostgreSQL", _host, ex.Message), ex);
        }
    }

    public async Task Disconnect()
    {
        if (_connection?.State == System.Data.ConnectionState.Open)
        {
            await _connection.CloseAsync();
            Console.WriteLine("Disconnected from PostgreSQL database.");
        }
    }

    public async Task<string?> Backup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default)
    {
        if (type == BackupType.Full)
            return await FullBackup(backupId, backupFilePath, cancellationToken);

        if (parent is null)
            throw new InvalidOperationException($"Cannot create a {type} PostgreSQL backup without a parent full backup.");

        return await DeltaBackup(backupFilePath, type, parent.FullBackupId, cancellationToken);
    }

    private async Task<string?> FullBackup(string backupId, string backupFilePath, CancellationToken cancellationToken)
    {
        var backupCommand = $"pg_dump --file \"{backupFilePath}\" --dbname \"{DbNameConnectionString()}\"";
        await ProcessRunner.RunAsync("pg_dump", backupCommand, cancellationToken, PgPasswordEnvironment());
        Console.WriteLine($"Backup created at {backupFilePath}");

        // Every full backup opens the pair of logical replication slots its own incremental
        // (consuming) and differential (peeking) chain will read from, so later deltas only need
        // this full backup's id to find them.
        await CreateSlotAsync(IncrementalSlotName(backupId), cancellationToken);
        await CreateSlotAsync(DifferentialSlotName(backupId), cancellationToken);
        return null;
    }

    private async Task CreateSlotAsync(string slotName, CancellationToken cancellationToken)
    {
        await using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT pg_create_logical_replication_slot($1, 'wal2json')";
        command.Parameters.AddWithValue(slotName);
        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == "42710") // duplicate_object: slot already exists.
        {
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not create logical replication slot '{slotName}'. Incremental/differential backups require " +
                "'wal_level = logical' on the server and the 'wal2json' output plugin installed. " +
                $"Original error: {ex.Message}", ex);
        }
    }

    // Incremental consumes (advances) its slot, so each call only returns changes since the
    // previous backup of any type. Differential peeks its slot without advancing, so every call
    // returns everything since the full backup, regardless of how many differentials came before.
    private async Task<string?> DeltaBackup(string backupFilePath, BackupType type, string fullBackupId, CancellationToken cancellationToken)
    {
        var slotName = type == BackupType.Incremental ? IncrementalSlotName(fullBackupId) : DifferentialSlotName(fullBackupId);
        var function = type == BackupType.Incremental ? "pg_logical_slot_get_changes" : "pg_logical_slot_peek_changes";

        var statements = new List<string>();
        await using (var command = _connection!.CreateCommand())
        {
            command.CommandText = $"SELECT data FROM {function}($1, NULL, NULL)";
            command.Parameters.AddWithValue(slotName);
            try
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    statements.AddRange(Wal2JsonTranslator.ToSqlStatements(reader.GetString(0)));
            }
            catch (PostgresException ex) when (ex.SqlState == "42704") // undefined_object: slot not found.
            {
                throw new InvalidOperationException(
                    $"Replication slot '{slotName}' was not found. Take a new full backup before creating " +
                    $"{type.ToString().ToLowerInvariant()} backups.", ex);
            }
        }

        var directory = Path.GetDirectoryName(backupFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllLinesAsync(backupFilePath, statements, cancellationToken);
        Console.WriteLine($"Backup created at {backupFilePath}");
        return null;
    }

    private static string IncrementalSlotName(string backupId) => $"dbbu_{Sanitize(backupId)}_inc";
    private static string DifferentialSlotName(string backupId) => $"dbbu_{Sanitize(backupId)}_diff";
    private static string Sanitize(string id) => new(id.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    public async Task Restore(string backupFilePath, BackupType type = BackupType.Full, IReadOnlyList<string>? targets = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveFilePath = backupFilePath;
        string? filteredTempFile = null;
        if (targets is { Count: > 0 })
        {
            var content = await File.ReadAllTextAsync(backupFilePath, cancellationToken);
            filteredTempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(filteredTempFile, SqlDumpTableFilter.FilterByTables(content, targets), cancellationToken);
            effectiveFilePath = filteredTempFile;
        }

        try
        {
            // Full dumps and reconstructed incremental/differential SQL files are both applied the
            // same way.
            var restoreCommand = $"psql --file \"{effectiveFilePath}\" --dbname \"{DbNameConnectionString()}\"";
            await ProcessRunner.RunAsync("psql", restoreCommand, cancellationToken, PgPasswordEnvironment());
            Console.WriteLine($"Database restored from {backupFilePath}");
        }
        finally
        {
            if (filteredTempFile is not null)
                File.Delete(filteredTempFile);
        }
    }

    private string DbNameConnectionString()
    {
        var portSegment = _port.HasValue ? $"Port={_port};" : string.Empty;
        return $"Host={_host};{portSegment}Database={_database};Username={_username}";
    }

    private Dictionary<string, string> PgPasswordEnvironment() => new() { ["PGPASSWORD"] = _password };
}
