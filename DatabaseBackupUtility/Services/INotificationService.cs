namespace DatabaseBackupUtility.Configs;

public interface INotificationService
{
    void SendNotification(string message);
}