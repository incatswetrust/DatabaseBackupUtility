using DatabaseBackupUtility.Models;
using FluentValidation;

namespace DatabaseBackupUtility.Configs;

public class NotificationConfigValidator:AbstractValidator<Notifications>
{
    public NotificationConfigValidator()
    {
        RuleFor(c=> c.SlackWebhookUrl).NotEmpty().WithMessage("Slack webhook url is required.");
    }
}