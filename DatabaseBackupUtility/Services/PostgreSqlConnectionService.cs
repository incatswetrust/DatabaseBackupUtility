using System.Diagnostics;
using Npgsql;

namespace DatabaseBackupUtility.Configs;

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

    public async Task< bool> TestConnection()
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

    public async Task Backup(string backupFilePath)
    {
        var dbConnectionString = $"Host={_host};Database={_database};Username={_username}";
        var backupCommand = $"pg_dump --file \"{backupFilePath}\" --dbname \"{dbConnectionString}\"";
        await ExecuteCommand(backupCommand);
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    public async Task Restore(string backupFilePath)
    {
        var dbConnectionString = $"Host={_host};Database={_database};Username={_username}";
        var restoreCommand = $"psql --file \"{backupFilePath}\" --dbname \"{dbConnectionString}\"";
        await ExecuteCommand(restoreCommand);
        Console.WriteLine($"Database restored from {backupFilePath}");
    }

    private async Task ExecuteCommand(string command)
    {
        var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardError = true,
            UseShellExecute = false
        };
        processInfo.Environment["PGPASSWORD"] = _password;

        using var process = Process.Start(processInfo)!;
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Command failed (exit {process.ExitCode}): {error}");
        }
    }
}