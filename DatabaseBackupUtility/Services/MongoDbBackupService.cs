using System.Globalization;

namespace DatabaseBackupUtility.Configs;

public class MongoDbBackupService(IDatabaseConnection dbConnection) : IBackupService
{
    public async Task CreateBackup(string backupFilePath)
    {
        await dbConnection.Connect();
        await dbConnection.Backup(backupFilePath);
        await dbConnection.Disconnect();

    }
}