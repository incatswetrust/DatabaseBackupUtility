using System.Diagnostics;
using MySqlConnector;

namespace DatabaseBackupUtility.Configs;

public class MySqlConnectionService : IDatabaseConnection
{
    private readonly string _host;
    private readonly string _database;
    private readonly string _username;
    private readonly string _password;
    private readonly MySqlConnection _connection;

    public MySqlConnectionService(string host, string database, string username, string password)
    {
        _host = host;
        _database = database;
        _username = username;
        _password = password;
        _connection = new MySqlConnection(GetConnectionString());
    }

    private string GetConnectionString()
    {
        return $"Server={_host};Database={_database};User={_username};Password={_password};";
    }

    public async Task <bool> TestConnection()
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
        await _connection.ChangeDatabaseAsync(_database);
        Console.WriteLine("Connected to MySQL database.");
    }
    
    public async Task Disconnect()
    {
        if (_connection.State != System.Data.ConnectionState.Open) return;
        await _connection.CloseAsync();
        Console.WriteLine("Disconnected from MySQL database.");
    }

    public async Task Backup(string backupFilePath)
    {
        await Task.Run(() =>
        {
            var backupCommand =
                $"mysqldump --databases {_database} --user={_username} --password={_password} > {backupFilePath}";
            ExecuteCommand(backupCommand);
            Console.WriteLine($"Backup created at {backupFilePath}");
        });
    }

    public async Task Restore(string backupFilePath)
    {
        await Task.Run(() =>
        {
            var restoreCommand =
                $"mysql --database={_database} --user={_username} --password={_password} < {backupFilePath}";
            ExecuteCommand(restoreCommand);
            Console.WriteLine($"Database restored from {backupFilePath}");
        });
    }

    private void ExecuteCommand(string command)
    {
        var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        process?.WaitForExit();

        var output = process?.StandardOutput.ReadToEnd();
        var error = process?.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(output))
        {
            Console.WriteLine(output);
        }

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"Error: {error}");
        }
    }
}
