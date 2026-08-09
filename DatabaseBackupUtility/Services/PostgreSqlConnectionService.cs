using Npgsql;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class PostgreSqlConnectionService : IDatabaseConnection
{
    private readonly string _connectionString;
    private readonly string _host;
    private readonly string _database;
    private readonly string _username;
    private readonly string _password;
    private NpgsqlConnection _connection;

    public PostgreSqlConnectionService(string host, string database, string username, string password)
    {
        _host = host;
        _database = database;
        _username = username;
        _password = password;
        _connectionString = $"Host={host};Database={database};Username={username};Password={password};";
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
        _connection = new NpgsqlConnection(_connectionString);
        await _connection.OpenAsync();
        Console.WriteLine("Connected to PostgreSQL database.");
    }

    public async Task Disconnect()
    {
        if (_connection.State == System.Data.ConnectionState.Open)
        {
            await _connection.CloseAsync();
            Console.WriteLine("Disconnected from PostgreSQL database.");
        }
    }

    public async Task Backup(string backupFilePath, CancellationToken cancellationToken = default)
    {
        var dbConnectionString = $"Host={_host};Database={_database};Username={_username}";
        var backupCommand = $"pg_dump --file \"{backupFilePath}\" --dbname \"{dbConnectionString}\"";
        await ProcessRunner.RunAsync("pg_dump", backupCommand, cancellationToken, PgPasswordEnvironment());
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    public async Task Restore(string backupFilePath, CancellationToken cancellationToken = default)
    {
        var dbConnectionString = $"Host={_host};Database={_database};Username={_username}";
        var restoreCommand = $"psql --file \"{backupFilePath}\" --dbname \"{dbConnectionString}\"";
        await ProcessRunner.RunAsync("psql", restoreCommand, cancellationToken, PgPasswordEnvironment());
        Console.WriteLine($"Database restored from {backupFilePath}");
    }

    private Dictionary<string, string> PgPasswordEnvironment() => new() { ["PGPASSWORD"] = _password };
}