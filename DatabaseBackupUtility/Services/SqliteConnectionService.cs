using Microsoft.Data.Sqlite;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class SqliteConnectionService : IDatabaseConnection
{
    private readonly string _databasePath;
    private SqliteConnection? _connection;

    public SqliteConnectionService(string databasePath)
    {
        _databasePath = databasePath;
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
            if (!File.Exists(_databasePath))
                throw new FileNotFoundException($"SQLite database file not found: {_databasePath}");

            _connection = new SqliteConnection($"Data Source={_databasePath}");
            await _connection.OpenAsync();
            Console.WriteLine("Connected to SQLite database.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.DescribeConnectionFailure("SQLite", _databasePath, ex.Message), ex);
        }
    }

    public async Task Disconnect()
    {
        if (_connection is null) return;
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        _connection = null;
        Console.WriteLine("Disconnected from SQLite database.");
    }

    // Takes a consistent snapshot of the database file via VACUUM INTO, which works safely even
    // while other connections are active against the source file.
    public async Task Backup(string backupFilePath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(backupFilePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        if (File.Exists(backupFilePath))
            File.Delete(backupFilePath);

        await using var connection = new SqliteConnection($"Data Source={_databasePath}");
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "VACUUM INTO $path;";
        command.Parameters.AddWithValue("$path", backupFilePath);
        await command.ExecuteNonQueryAsync(cancellationToken);
        Console.WriteLine($"Backup created at {backupFilePath}");
    }

    // A SQLite database is a single file, so restoring means replacing that file with the backup.
    public async Task Restore(string backupFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException($"Backup file not found: {backupFilePath}");

        var wasConnected = _connection is not null;
        if (wasConnected)
            await Disconnect();

        SqliteConnection.ClearAllPools();
        File.Copy(backupFilePath, _databasePath, overwrite: true);
        Console.WriteLine($"Database restored from {backupFilePath}");

        if (wasConnected)
            await Connect();
    }
}
