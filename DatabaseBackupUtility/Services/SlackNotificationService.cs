using System.Text;

namespace DatabaseBackupUtility.Configs;

public class SlackNotificationService : INotificationService
{
    private readonly string _webhookUrl;

    public SlackNotificationService(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
    }

    public async void SendNotification(string message)
    {
        using var httpClient = new HttpClient();
        var payload = new { text = message };
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(_webhookUrl, content);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Failed to send Slack notification: {response.StatusCode}");
        }
    }
}