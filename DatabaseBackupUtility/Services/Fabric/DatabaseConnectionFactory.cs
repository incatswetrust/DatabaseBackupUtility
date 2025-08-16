using DatabaseBackupUtility.Models;
using Microsoft.Extensions.Configuration;

namespace DatabaseBackupUtility.Configs;

interface IDatabaseConnectionFactory
{
    IDatabaseConnection CreateConnection(DatabaseConfig dbConfig);
}

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    public IDatabaseConnection CreateConnection(DatabaseConfig dbConfig)
    {
        var dbType = dbConfig.Type;
        return dbType switch
        {
            "MySql" => new MySqlConnectionService(dbConfig.Host,
                dbConfig.DatabaseName, dbConfig.Username,
                dbConfig.Password),
            "PostgreSql" => new PostgreSqlConnectionService(dbConfig.Host,
                dbConfig.DatabaseName, dbConfig.Username,
                dbConfig.Password),
            "MongoDb" => new MongoDbConnectionService(
                $"mongodb://{dbConfig.Username}:{dbConfig.Password}@{dbConfig.Host}",
                dbConfig.DatabaseName),
            _ => throw new Exception("Unsupported database type.")
        };
    }
}