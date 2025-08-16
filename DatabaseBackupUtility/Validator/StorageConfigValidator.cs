using DatabaseBackupUtility.Models;
using FluentValidation;

namespace DatabaseBackupUtility.Configs;

public class StorageConfigValidator:AbstractValidator<Storage>
{
    public StorageConfigValidator()
    {
        RuleFor(c=> c.Type).NotEmpty().WithMessage("Storage type is required.");
    }
}