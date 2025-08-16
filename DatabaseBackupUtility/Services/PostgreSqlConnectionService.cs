using Npgsql;

namespace DatabaseBackupUtility.Configs;

public class PostgreSqlConnectionService : IDatabaseConnection
{
    private readonly string _connectionString;
    private NpgsqlConnection _connection;

    public PostgreSqlConnectionService(string host, string database, string username, string password)
    {
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
        await Task.Run(() =>
        {
            var backupCommand = $"pg_dump --file \"{backupFilePath}\" --dbname \"{_connectionString}\"";
            System.Diagnostics.Process.Start("bash", $"-c \"{backupCommand}\"");
            Console.WriteLine($"Backup created at {backupFilePath}");
        });
    }

    public async Task Restore(string backupFilePath)
    {
        await Task.Run(() =>
        {
            var restoreCommand = $"psql --file \"{backupFilePath}\" --dbname \"{_connectionString}\"";
            System.Diagnostics.Process.Start("bash", $"-c \"{restoreCommand}\"");
            Console.WriteLine($"Database restored from {backupFilePath}");
        });
    }
}