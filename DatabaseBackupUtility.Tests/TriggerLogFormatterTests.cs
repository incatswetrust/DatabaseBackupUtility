using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class TriggerLogFormatterTests
{
    [Fact]
    public void Format_TagsMessageWithScheduledTrigger()
    {
        var result = TriggerLogFormatter.Format(TriggerLogFormatter.Scheduled, "Starting full backup process...");

        Assert.Equal("[trigger: scheduled] Starting full backup process...", result);
    }

    [Fact]
    public void Format_TagsMessageWithManualTrigger()
    {
        var result = TriggerLogFormatter.Format(TriggerLogFormatter.Manual, "Starting full backup process...");

        Assert.Equal("[trigger: manual] Starting full backup process...", result);
    }
}
