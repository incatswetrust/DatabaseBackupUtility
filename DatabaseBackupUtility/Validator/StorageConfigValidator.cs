using DatabaseBackupUtility.Models;
using FluentValidation;

namespace DatabaseBackupUtility.Configs;

public class StorageConfigValidator:AbstractValidator<Storage>
{
    public StorageConfigValidator()
    {
        RuleFor(c=> c.Type).NotEmpty().WithMessage("Storage type is required.");

        When(c => c.Type == "Local", () =>
        {
            RuleFor(c => c.LocalPath).NotEmpty().WithMessage("LocalPath is required for Local storage.");
        });

        When(c => c.Type != "Local", () =>
        {
            RuleFor(c => c.Cloud.Provider).NotEmpty().WithMessage("Cloud provider is required.");
            RuleFor(c => c.Cloud.BucketName).NotEmpty().WithMessage("Bucket name is required.");
        });
    }
}