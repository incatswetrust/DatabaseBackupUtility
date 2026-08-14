namespace DatabaseBackupUtility.Services;

// Tags a log/notification message with what started the backup, so scheduled runs (from the
// `schedule` command) are distinguishable from manually-triggered ones in the log file.
internal static class TriggerLogFormatter
{
    public const string Manual = "manual";
    public const string Scheduled = "scheduled";

    public static string Format(string trigger, string message) => $"[trigger: {trigger}] {message}";
}
