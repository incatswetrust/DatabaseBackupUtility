using DatabaseBackupUtility.Models;
using FluentValidation;

namespace DatabaseBackupUtility.Configs;

public class CloudStorageConfigValidator:AbstractValidator<CloudStorageConfig>
{
    public CloudStorageConfigValidator()
    {
        RuleFor(c=> c.Provider).NotEmpty().WithMessage("Cloud provider is required.");
        RuleFor(c=> c.BucketName).NotEmpty().WithMessage("Bucket name is required.");
    }
}