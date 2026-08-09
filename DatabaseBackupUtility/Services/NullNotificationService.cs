using DatabaseBackupUtility.Services.Interfaces;

namespace DatabaseBackupUtility.Services;

public class NullNotificationService : INotificationService
{
    public Task SendNotification(string message) => Task.CompletedTask;
}
