using DatabaseBackupUtility.Services;
using Microsoft.Data.Sqlite;

namespace DatabaseBackupUtility.Tests;

public class SqliteConnectionServiceTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"sqlite_test_{Guid.NewGuid():N}.db");
    private readonly string _backupPath = Path.Combine(Path.GetTempPath(), $"sqlite_test_backup_{Guid.NewGuid():N}.db");

    public SqliteConnectionServiceTests()
    {
        using var connection = new SqliteConnection($"Data Source={_databasePath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY, name TEXT); INSERT INTO items (name) VALUES ('first');";
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task TestConnection_ReturnsTrueForExistingDatabaseFile()
    {
        var service = new SqliteConnectionService(_databasePath);

        Assert.True(await service.TestConnection());
    }

    [Fact]
    public async Task TestConnection_ReturnsFalseForMissingDatabaseFile()
    {
        var service = new SqliteConnectionService(Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.db"));

        Assert.False(await service.TestConnection());
    }

    [Fact]
    public async Task Backup_CreatesFileContainingSourceData()
    {
        var service = new SqliteConnectionService(_databasePath);

        await service.Backup(_backupPath);

        Assert.True(File.Exists(_backupPath));
        Assert.Equal("first", await ReadFirstItemName(_backupPath));
    }

    [Fact]
    public async Task Restore_ReplacesDatabaseFileWithBackupContents()
    {
        var service = new SqliteConnectionService(_databasePath);
        await service.Backup(_backupPath);
        await AddItem(_databasePath, "second");

        await service.Restore(_backupPath);

        Assert.Equal("first", await ReadFirstItemName(_databasePath));
    }

    private static async Task<string?> ReadFirstItemName(string databasePath)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM items ORDER BY id LIMIT 1;";
        return (string?)await command.ExecuteScalarAsync();
    }

    private static async Task AddItem(string databasePath, string name)
    {
        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO items (name) VALUES ($name);";
        command.Parameters.AddWithValue("$name", name);
        await command.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
        File.Delete(_backupPath);
    }
}
