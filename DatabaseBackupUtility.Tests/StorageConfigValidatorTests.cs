using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Validators;

namespace DatabaseBackupUtility.Tests;

public class StorageConfigValidatorTests
{
    private readonly StorageConfigValidator _validator = new();

    [Fact]
    public void Validate_AcceptsLocalStorageWithPath()
    {
        var config = new Storage { Type = "Local", LocalPath = "/var/backups" };

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsLocalStorageWithoutPath()
    {
        var config = new Storage { Type = "Local", LocalPath = string.Empty };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AcceptsCloudStorageWithProviderAndBucket()
    {
        var config = new Storage
        {
            Type = "S3",
            Cloud = new CloudStorageConfig { Provider = "AWS", BucketName = "my-bucket" }
        };

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsCloudStorageWithoutBucketName()
    {
        var config = new Storage
        {
            Type = "S3",
            Cloud = new CloudStorageConfig { Provider = "AWS", BucketName = string.Empty }
        };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsCloudStorageWithoutProvider()
    {
        var config = new Storage
        {
            Type = "S3",
            Cloud = new CloudStorageConfig { Provider = string.Empty, BucketName = "my-bucket" }
        };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyType()
    {
        var config = new Storage { Type = string.Empty, LocalPath = "/var/backups" };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DoesNotRequireLocalPathForCloudStorage()
    {
        var config = new Storage
        {
            Type = "Azure",
            LocalPath = string.Empty,
            Cloud = new CloudStorageConfig { Provider = "Azure", BucketName = "container" }
        };

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }
}
