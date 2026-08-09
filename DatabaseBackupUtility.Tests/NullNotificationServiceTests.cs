using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class NullNotificationServiceTests
{
    [Fact]
    public async Task SendNotification_CompletesWithoutError()
    {
        var service = new NullNotificationService();

        await service.SendNotification("any message");
    }

    [Fact]
    public void SendNotification_ReturnsCompletedTask()
    {
        var service = new NullNotificationService();

        var task = service.SendNotification("any message");

        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task SendNotification_AcceptsEmptyMessage()
    {
        var service = new NullNotificationService();

        await service.SendNotification(string.Empty);
    }
}
