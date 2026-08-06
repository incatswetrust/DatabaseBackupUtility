namespace DatabaseBackupUtility.Configs;

public class NullNotificationService : INotificationService
{
    public Task SendNotification(string message) => Task.CompletedTask;
}
