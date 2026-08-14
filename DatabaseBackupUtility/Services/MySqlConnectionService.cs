using MySqlConnector;
using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class MySqlConnectionService : IDatabaseConnection
{
    private readonly string _host;
    private readonly int? _port;
    private readonly string _database;
    private readonly string _username;
    private readonly string _password;
    private readonly MySqlConnection _connection;

    public MySqlConnectionService(string host, int? port, string database, string username, string password)
    {
        _host = host;
        _port = port;
        _database = database;
        _username = username;
        _password = password;
        _connection = new MySqlConnection(GetConnectionString());
    }

    private string GetConnectionString()
    {
        var portSegment = _port.HasValue ? $"Port={_port};" : string.Empty;
        return $"Server={_host};{portSegment}Database={_database};User={_username};Password={_password};";
    }

    private string HostArguments()
    {
        var portArgument = _port.HasValue ? $" --port={_port}" : string.Empty;
        return $"--host={_host}{portArgument}";
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
            await _connection.OpenAsync();
            Console.WriteLine("Connected to MySQL database.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(ErrorMessages.DescribeConnectionFailure("MySQL", _host, ex.Message), ex);
        }
    }

    public async Task Disconnect()
    {
        if (_connection.State != System.Data.ConnectionState.Open) return;
        await _connection.CloseAsync();
        Console.WriteLine("Disconnected from MySQL database.");
    }

    public async Task<string?> Backup(string backupId, string backupFilePath, BackupType type = BackupType.Full,
        BackupParent? parent = null, CancellationToken cancellationToken = default)
    {
        if (type == BackupType.Full)
            return await FullBackup(backupFilePath, cancellationToken);

        if (parent is null)
            throw new InvalidOperationException($"Cannot create a {type} MySQL backup without a parent backup.");

        return await IncrementalBackup(backupFilePath, parent.Position, cancellationToken);
    }

    private async Task<string?> FullBackup(string backupFilePath, CancellationToken cancellationToken)
    {
        var cnfPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(cnfPath, $"[client]\npassword={_password}\n", cancellationToken);
            var backupCommand =
                $"mysqldump --defaults-extra-file={cnfPath} {HostArguments()} --databases {_database} --user={_username} > {backupFilePath}";
            await ProcessRunner.RunAsync("mysqldump", backupCommand, cancellationToken);
            Console.WriteLine($"Backup created at {backupFilePath}");
            return await GetBinlogPositionAsync(cancellationToken);
        }
        finally
        {
            File.Delete(cnfPath);
        }
    }

    // Streams binlog events since the parent's position via mysqlbinlog's remote-server mode, so
    // no filesystem access to the MySQL server is required (only REPLICATION SLAVE/CLIENT
    // privileges). The output is plain SQL, replayable through `mysql <file` exactly like a full
    // dump.
    private async Task<string?> IncrementalBackup(string backupFilePath, string? sincePosition, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sincePosition))
            throw new InvalidOperationException("Cannot create an incremental/differential MySQL backup: the parent backup has no recorded binlog position.");

        var (fromFile, fromPosition) = ParsePosition(sincePosition);
        var logFiles = await GetBinaryLogFilesFromAsync(fromFile, cancellationToken);
        if (logFiles.Count == 0)
            throw new InvalidOperationException($"Binary log file '{fromFile}' is no longer available on the server; take a new full backup.");

        var cnfPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(cnfPath, $"[client]\npassword={_password}\n", cancellationToken);
            var files = string.Join(' ', logFiles);
            var backupCommand =
                $"mysqlbinlog --defaults-extra-file={cnfPath} --read-from-remote-server {HostArguments()} --user={_username} --start-position={fromPosition} {files} > {backupFilePath}";
            await ProcessRunner.RunAsync("mysqlbinlog", backupCommand, cancellationToken);
            Console.WriteLine($"Backup created at {backupFilePath}");
            return await GetBinlogPositionAsync(cancellationToken);
        }
        finally
        {
            File.Delete(cnfPath);
        }
    }

    private async Task<string?> GetBinlogPositionAsync(CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SHOW BINARY LOG STATUS";
        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? $"{reader.GetString(0)}:{reader.GetInt64(1)}" : null;
        }
        catch (MySqlException)
        {
            // MySQL < 8.4 and MariaDB use the older statement name.
            command.CommandText = "SHOW MASTER STATUS";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? $"{reader.GetString(0)}:{reader.GetInt64(1)}" : null;
        }
    }

    private async Task<List<string>> GetBinaryLogFilesFromAsync(string fromFile, CancellationToken cancellationToken)
    {
        await using var command = _connection.CreateCommand();
        command.CommandText = "SHOW BINARY LOGS";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var files = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            files.Add(reader.GetString(0));

        return files.Where(f => string.CompareOrdinal(f, fromFile) >= 0)
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
    }

    private static (string file, long position) ParsePosition(string position)
    {
        var parts = position.Split(':', 2);
        return (parts[0], long.Parse(parts[1]));
    }

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

        var cnfPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(cnfPath, $"[client]\npassword={_password}\n", cancellationToken);
            var restoreCommand =
                $"mysql --defaults-extra-file={cnfPath} {HostArguments()} --database={_database} --user={_username} < {effectiveFilePath}";
            await ProcessRunner.RunAsync("mysql", restoreCommand, cancellationToken);
            Console.WriteLine($"Database restored from {backupFilePath}");
        }
        finally
        {
            File.Delete(cnfPath);
            if (filteredTempFile is not null)
                File.Delete(filteredTempFile);
        }
    }
}
