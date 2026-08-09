using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Validators;

namespace DatabaseBackupUtility.Tests;

public class CloudStorageConfigValidatorTests
{
    private readonly CloudStorageConfigValidator _validator = new();

    [Fact]
    public void Validate_AcceptsProviderAndBucketName()
    {
        var config = new CloudStorageConfig { Provider = "AWS", BucketName = "my-bucket" };

        var result = _validator.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyProvider()
    {
        var config = new CloudStorageConfig { Provider = string.Empty, BucketName = "my-bucket" };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejectsEmptyBucketName()
    {
        var config = new CloudStorageConfig { Provider = "AWS", BucketName = string.Empty };

        var result = _validator.Validate(config);

        Assert.False(result.IsValid);
    }
}
