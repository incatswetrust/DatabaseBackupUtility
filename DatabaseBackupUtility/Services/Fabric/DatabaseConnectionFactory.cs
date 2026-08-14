using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services;
using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Factories;

interface IDatabaseConnectionFactory
{
    IDatabaseConnection CreateConnection(DatabaseConfig dbConfig);
}

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    public IDatabaseConnection CreateConnection(DatabaseConfig dbConfig)
    {
        var dbType = dbConfig.Type;
        var hostAndPort = dbConfig.Port.HasValue ? $"{dbConfig.Host}:{dbConfig.Port}" : dbConfig.Host;
        return dbType switch
        {
            "MySql" => new MySqlConnectionService(dbConfig.Host, dbConfig.Port,
                dbConfig.DatabaseName, dbConfig.Username,
                dbConfig.Password),
            "PostgreSql" => new PostgreSqlConnectionService(dbConfig.Host, dbConfig.Port,
                dbConfig.DatabaseName, dbConfig.Username,
                dbConfig.Password),
            "MongoDb" => new MongoDbConnectionService(
                $"mongodb://{dbConfig.Username}:{dbConfig.Password}@{hostAndPort}",
                dbConfig.DatabaseName),
            "Sqlite" => new SqliteConnectionService(dbConfig.FilePath!),
            _ => throw new Exception("Unsupported database type.")
        };
    }
}