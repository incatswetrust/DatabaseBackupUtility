namespace DatabaseBackupUtility.Models;

// Chain context handed to a connection service when it's about to take an Incremental or
// Differential backup: which Full backup the chain is rooted at, and the checkpoint to capture
// changes since (the immediate parent's Position for Incremental, the Full's own Position for
// Differential).
public record BackupParent(string FullBackupId, string? Position);
