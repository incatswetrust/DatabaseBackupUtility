namespace DatabaseBackupUtility.Services.Interfaces;

public interface INotificationService
{
    Task SendNotification(string message);
}