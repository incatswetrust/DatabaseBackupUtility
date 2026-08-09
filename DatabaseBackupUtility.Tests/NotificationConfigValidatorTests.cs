using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Validators;

namespace DatabaseBackupUtility.Tests;

public class NotificationConfigValidatorTests
{
    private readonly NotificationConfigValidator _validator = new();

    [Fact]
    public void Validate_AcceptsWebhookUrl()
    {
        var config = new Notifications { SlackWebhookUrl = "https://hooks.slack.com/services/x" };

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyWebhookUrl()
    {
        var config = new Notifications { SlackWebhookUrl = string.Empty };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }
}
