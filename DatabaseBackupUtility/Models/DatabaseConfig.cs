namespace DatabaseBackupUtility.Models;

public class DatabaseConfig
{
    public string Type { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Used instead of Host/Port/Username/Password when Type is "Sqlite", since a SQLite database is a single file.
    public string? FilePath { get; set; }
}






