using DatabaseBackupUtility.Models;
using FluentValidation;

namespace DatabaseBackupUtility.Validators;

public class NotificationConfigValidator : AbstractValidator<Notifications>
{
    public NotificationConfigValidator()
    {
        RuleFor(c => c.SlackWebhookUrl).NotEmpty().WithMessage("Slack webhook url is required.");
    }
}