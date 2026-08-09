using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Validators;

namespace DatabaseBackupUtility.Tests;

public class DatabaseConfigValidatorTests
{
    private readonly DatabaseConfigValidator _validator = new();

    private static DatabaseConfig ValidConfig() => new()
    {
        Type = "MySql",
        Host = "localhost",
        DatabaseName = "mydb",
        Username = "root",
        Password = "secret"
    };

    [Fact]
    public void Validate_AcceptsFullyPopulatedConfig()
    {
        var result = _validator.Validate(ValidConfig());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("MySql")]
    [InlineData("PostgreSql")]
    [InlineData("MongoDb")]
    public void Validate_AcceptsSupportedTypes(string type)
    {
        var config = ValidConfig();
        config.Type = type;

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Oracle")]
    [InlineData("Redis")]
    public void Validate_RejectsUnsupportedTypes(string type)
    {
        var config = ValidConfig();
        config.Type = type;

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyHost()
    {
        var config = ValidConfig();
        config.Host = string.Empty;

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyDatabaseName()
    {
        var config = ValidConfig();
        config.DatabaseName = string.Empty;

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyUsername()
    {
        var config = ValidConfig();
        config.Username = string.Empty;

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyPassword()
    {
        var config = ValidConfig();
        config.Password = string.Empty;

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AcceptsMissingPort()
    {
        var config = ValidConfig();
        config.Port = null;

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3306)]
    [InlineData(65535)]
    public void Validate_AcceptsPortWithinRange(int port)
    {
        var config = ValidConfig();
        config.Port = port;

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_RejectsPortOutsideRange(int port)
    {
        var config = ValidConfig();
        config.Port = port;

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }
}
