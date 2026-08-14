using System.Text.Json;
using DatabaseBackupUtility.Models;

namespace DatabaseBackupUtility.Services;

// Persists and resolves the chain of full/incremental/differential backups for a database, using
// a JSON sidecar file per backup stored locally alongside (or independently of) the backup blobs
// themselves. Kept local even when the backup blob goes to cloud storage, since IStorageService
// has no listing capability to discover existing backups from a remote provider.
public class BackupChainService(string metadataDirectory)
{
    private string ManifestPath(string id) => Path.Combine(metadataDirectory, $"{id}.backup.json");

    public async Task SaveManifestAsync(BackupManifest manifest, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(metadataDirectory);
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ManifestPath(manifest.Id), json, cancellationToken);
    }

    public async Task<List<BackupManifest>> LoadManifestsAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(metadataDirectory)) return [];

        var manifests = new List<BackupManifest>();
        foreach (var file in Directory.GetFiles(metadataDirectory, "*.backup.json"))
        {
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var manifest = JsonSerializer.Deserialize<BackupManifest>(json);
            if (manifest is not null && manifest.DatabaseName == databaseName)
                manifests.Add(manifest);
        }

        return manifests.OrderBy(m => m.CreatedAtUtc).ToList();
    }

    // What a connection service needs to capture an Incremental/Differential backup: the nearest
    // Full ancestor's id, and the checkpoint to record changes since.
    public static BackupParent? ResolveParent(IReadOnlyList<BackupManifest> manifests, BackupType type)
    {
        if (type == BackupType.Full) return null;

        var reference = FindReference(manifests, type);
        if (reference is null)
            throw new InvalidOperationException($"Cannot create a {type} backup: no previous full backup was found. Take a full backup first.");

        return new BackupParent(reference.FullBackupId, reference.Position);
    }

    // The manifest id an Incremental/Differential backup should record as its ParentId.
    public static string? ResolveImmediateParentId(IReadOnlyList<BackupManifest> manifests, BackupType type) =>
        type == BackupType.Full ? null : FindReference(manifests, type)?.Id;

    private static BackupManifest? FindReference(IReadOnlyList<BackupManifest> manifests, BackupType type) =>
        type == BackupType.Incremental
            // Incremental: changes since the most recent backup of any type.
            ? manifests.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault()
            // Differential: always relative to the most recent full backup, never another differential.
            : manifests.Where(m => m.Type == BackupType.Full).OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();

    public static BackupManifest? FindLatest(IReadOnlyList<BackupManifest> manifests) =>
        manifests.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();

    // Walks ParentId back from the target to its Full ancestor, returning the chain in
    // chronological (apply) order: [Full, ...intermediate incrementals..., target].
    public static List<BackupManifest> ResolveChain(IReadOnlyList<BackupManifest> manifests, string targetId)
    {
        var byId = manifests.ToDictionary(m => m.Id);
        if (!byId.TryGetValue(targetId, out var current))
            throw new InvalidOperationException($"Backup '{targetId}' was not found.");

        var chain = new List<BackupManifest> { current };
        while (current.Type != BackupType.Full)
        {
            if (current.ParentId is null || !byId.TryGetValue(current.ParentId, out var parent))
                throw new InvalidOperationException($"Backup chain for '{targetId}' is broken: parent '{current.ParentId}' was not found.");
            chain.Add(parent);
            current = parent;
        }

        chain.Reverse();
        return chain;
    }
}
