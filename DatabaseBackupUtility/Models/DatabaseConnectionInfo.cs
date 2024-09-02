namespace DatabaseBackupUtility.Models;

public class DatabaseConnectionInfo
{
    public string Type { get; set; } 
    public string Host { get; set; }
    public string DatabaseName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}