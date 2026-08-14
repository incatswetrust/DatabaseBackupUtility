using DatabaseBackupUtility.Models;
using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class BackupChainServiceTests
{
    private static BackupManifest Manifest(string id, BackupType type, string? parentId, string fullBackupId,
        DateTime createdAt, string? position = null) => new()
    {
        Id = id,
        DatabaseName = "mydb",
        Type = type,
        ParentId = parentId,
        FullBackupId = fullBackupId,
        Position = position,
        FileName = $"backup_{id}.sql",
        CreatedAtUtc = createdAt
    };

    [Fact]
    public void ResolveParent_ReturnsNullForFull()
    {
        var result = BackupChainService.ResolveParent([], BackupType.Full);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveParent_ThrowsWhenNoFullBackupExists()
    {
        Assert.Throws<InvalidOperationException>(() =>
            BackupChainService.ResolveParent([], BackupType.Incremental));
        Assert.Throws<InvalidOperationException>(() =>
            BackupChainService.ResolveParent([], BackupType.Differential));
    }

    [Fact]
    public void ResolveParent_Incremental_UsesMostRecentBackupOfAnyType()
    {
        var full = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1), "pos-0");
        var inc1 = Manifest("inc-1", BackupType.Incremental, "full-1", "full-1", new DateTime(2024, 1, 2), "pos-1");
        var manifests = new List<BackupManifest> { full, inc1 };

        var parent = BackupChainService.ResolveParent(manifests, BackupType.Incremental);

        Assert.NotNull(parent);
        Assert.Equal("full-1", parent!.FullBackupId);
        Assert.Equal("pos-1", parent.Position);
    }

    [Fact]
    public void ResolveParent_Differential_AlwaysUsesMostRecentFullNotAnotherDifferential()
    {
        var full = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1), "pos-0");
        var diff1 = Manifest("diff-1", BackupType.Differential, "full-1", "full-1", new DateTime(2024, 1, 2), "pos-1");
        var manifests = new List<BackupManifest> { full, diff1 };

        var parent = BackupChainService.ResolveParent(manifests, BackupType.Differential);

        Assert.NotNull(parent);
        Assert.Equal("full-1", parent!.FullBackupId);
        Assert.Equal("pos-0", parent.Position);
    }

    [Fact]
    public void ResolveChain_ForFullBackup_ReturnsOnlyItself()
    {
        var full = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1));
        var manifests = new List<BackupManifest> { full };

        var chain = BackupChainService.ResolveChain(manifests, "full-1");

        Assert.Equal(["full-1"], chain.Select(m => m.Id));
    }

    [Fact]
    public void ResolveChain_ForIncremental_WalksBackThroughEveryIntermediateIncremental()
    {
        var full = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1));
        var inc1 = Manifest("inc-1", BackupType.Incremental, "full-1", "full-1", new DateTime(2024, 1, 2));
        var inc2 = Manifest("inc-2", BackupType.Incremental, "inc-1", "full-1", new DateTime(2024, 1, 3));
        var manifests = new List<BackupManifest> { full, inc1, inc2 };

        var chain = BackupChainService.ResolveChain(manifests, "inc-2");

        Assert.Equal(["full-1", "inc-1", "inc-2"], chain.Select(m => m.Id));
    }

    [Fact]
    public void ResolveChain_ForDifferential_SkipsEarlierIncrementalsAndOtherDifferentials()
    {
        var full = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1));
        var inc1 = Manifest("inc-1", BackupType.Incremental, "full-1", "full-1", new DateTime(2024, 1, 2));
        var diff1 = Manifest("diff-1", BackupType.Differential, "full-1", "full-1", new DateTime(2024, 1, 3));

        var manifests = new List<BackupManifest> { full, inc1, diff1 };

        var chain = BackupChainService.ResolveChain(manifests, "diff-1");

        Assert.Equal(["full-1", "diff-1"], chain.Select(m => m.Id));
    }

    [Fact]
    public void ResolveChain_ThrowsWhenTargetIsMissing()
    {
        Assert.Throws<InvalidOperationException>(() => BackupChainService.ResolveChain([], "missing"));
    }

    [Fact]
    public void ResolveChain_ThrowsWhenParentIsMissing()
    {
        var orphan = Manifest("inc-1", BackupType.Incremental, "missing-parent", "missing-parent", new DateTime(2024, 1, 1));

        Assert.Throws<InvalidOperationException>(() => BackupChainService.ResolveChain([orphan], "inc-1"));
    }

    [Fact]
    public void FindLatest_ReturnsMostRecentAcrossTypes()
    {
        var full = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1));
        var inc1 = Manifest("inc-1", BackupType.Incremental, "full-1", "full-1", new DateTime(2024, 1, 3));
        var diff1 = Manifest("diff-1", BackupType.Differential, "full-1", "full-1", new DateTime(2024, 1, 2));

        var latest = BackupChainService.FindLatest([full, inc1, diff1]);

        Assert.Equal("inc-1", latest!.Id);
    }

    [Fact]
    public async Task SaveAndLoadManifestsAsync_RoundTripsAndFiltersByDatabase()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"chain_test_{Guid.NewGuid():N}");
        try
        {
            var service = new BackupChainService(directory);
            var mine = Manifest("full-1", BackupType.Full, null, "full-1", new DateTime(2024, 1, 1));
            var other = new BackupManifest
            {
                Id = "full-2",
                DatabaseName = "otherdb",
                Type = BackupType.Full,
                FullBackupId = "full-2",
                FileName = "backup_full-2.sql",
                CreatedAtUtc = new DateTime(2024, 1, 1)
            };

            await service.SaveManifestAsync(mine);
            await service.SaveManifestAsync(other);

            var loaded = await service.LoadManifestsAsync("mydb");

            Assert.Single(loaded);
            Assert.Equal("full-1", loaded[0].Id);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
