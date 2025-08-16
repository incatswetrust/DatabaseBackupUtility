using System.Text;

namespace DatabaseBackupUtility.Configs;

public class SlackNotificationService(string webhookUrl) : INotificationService
{
    public async Task SendNotification(string message)
    {
        using var httpClient = new HttpClient();
        var payload = new { text = message };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to send Slack notification: {response.StatusCode}");
        }
    }
}