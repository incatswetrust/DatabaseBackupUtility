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

    public bool TestConnection()
    {
        try
        {
            Connect();
            Disconnect();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Connect()
    {
        _connection = new NpgsqlConnection(_connectionString);
        _connection.Open();
        Console.WriteLine("Connected to PostgreSQL database.");
    }

    public void Disconnect()
    {
        if (_connection != null && _connection.State == System.Data.ConnectionState.Open)
        {
            _connection.Close();
            Console.WriteLine("Disconnected from PostgreSQL database.");
        }
    }

    public void Backup(string backupFilePath)
    {
        var backupCommand = $"pg_dump --file \"{backupFilePath}\" --dbname \"{_connectionString}\"";
        System.Diagnostics.Process.Start("bash", $"-c \"{backupCommand}\"");
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    public void Restore(string backupFilePath)
    {
        var restoreCommand = $"psql --file \"{backupFilePath}\" --dbname \"{_connectionString}\"";
        System.Diagnostics.Process.Start("bash", $"-c \"{restoreCommand}\"");
        Console.WriteLine($"Database restored from {backupFilePath}");
    }
}