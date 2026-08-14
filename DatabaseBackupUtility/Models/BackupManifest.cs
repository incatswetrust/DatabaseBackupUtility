namespace DatabaseBackupUtility.Models;

// Sidecar record persisted next to a backup file, describing where it sits in a full/incremental/
// differential chain so a restore can work out which files to apply, and in what order.
public class BackupManifest
{
    public string Id { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public BackupType Type { get; set; }

    // Immediate predecessor this backup was taken against: the previous backup of any type for
    // Incremental, or the base full backup for Differential. Null for Full.
    public string? ParentId { get; set; }

    // Nearest Full ancestor's id (its own id when this manifest IS the Full backup). Lets a
    // connection service locate engine-specific chain state (e.g. a PostgreSQL replication slot)
    // without walking the whole chain.
    public string FullBackupId { get; set; } = string.Empty;

    // Opaque, engine-specific checkpoint captured right after this backup (e.g. a MySQL binlog
    // file:position or a MongoDB oplog timestamp). Null when the engine tracks position itself.
    public string? Position { get; set; }

    public string FileName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
