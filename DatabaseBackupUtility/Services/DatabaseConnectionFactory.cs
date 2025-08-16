using Microsoft.Extensions.Configuration;

namespace DatabaseBackupUtility.Configs;

interface IDatabaseConnectionFactory
{
    IDatabaseConnection CreateConnection(IConfiguration dbConfig);
}

public class DatabaseConnectionFactory : IDatabaseConnectionFactory
{
    public IDatabaseConnection CreateConnection(IConfiguration dbConfig)
    {
        var dbType = dbConfig.GetValue<string>("Type");

        return dbType switch
        {
            "MySql" => new MySqlConnectionService(dbConfig.GetValue<string>("Host")!,
                dbConfig.GetValue<string>("DatabaseName")!, dbConfig.GetValue<string>("Username")!,
                dbConfig.GetValue<string>("Password")!),
            "PostgreSql" => new PostgreSqlConnectionService(dbConfig.GetValue<string>("Host"),
                dbConfig.GetValue<string>("DatabaseName"), dbConfig.GetValue<string>("Username"),
                dbConfig.GetValue<string>("Password")),
            "MongoDb" => new MongoDbConnectionService(
                $"mongodb://{dbConfig.GetValue<string>("Username")}:{dbConfig.GetValue<string>("Password")}@{dbConfig.GetValue<string>("Host")}",
                dbConfig.GetValue<string>("DatabaseName")),
            _ => throw new Exception("Unsupported database type.")
        };
    }
}