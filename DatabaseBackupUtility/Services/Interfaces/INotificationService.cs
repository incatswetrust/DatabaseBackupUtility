namespace DatabaseBackupUtility.Configs;

public interface INotificationService
{
    Task SendNotification(string message);
}