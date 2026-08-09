using DatabaseBackupUtility.Models;
using FluentValidation;

namespace DatabaseBackupUtility.Validators;

public class DatabaseConfigValidator : AbstractValidator<DatabaseConfig>
{
    public DatabaseConfigValidator()
    {
        RuleFor(c => c.Type)
            .NotEmpty().WithMessage("Database type is required.")
            .Must(t => new[] { "MySql", "PostgreSql", "MongoDb" }.Contains(t))
            .WithMessage("Database type must be MySql, PostgreSql, or MongoDb.");
        RuleFor(c => c.Host).NotEmpty().WithMessage("Database host is required.");
        RuleFor(c => c.DatabaseName).NotEmpty().WithMessage("Database name is required.");
        RuleFor(c => c.Username).NotEmpty().WithMessage("Database username is required.");
        RuleFor(c => c.Password).NotEmpty().WithMessage("Database password is required.");
    }
}