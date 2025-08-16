namespace DatabaseBackupUtility.Configs;

public class CommandLineParser(string[]? args)
{
    public bool IsValid()
    {
        if (args == null || args.Length == 0)
        {
            ShowUsage();
            return false;
        }
        var command = args[0].ToLower();
        if (command is "backup" or "restore") return true;
        ShowUsage();
        return false;

    }

    public string GetCommand()
    {
        return args is { Length: > 0 } ? args[0].ToLower() : string.Empty;
    }

    public string? GetOption(string optionName)
    {
        if (args == null) return null;
        var index = Array.IndexOf(args, optionName);
        if (index >= 0 && index < args.Length - 1)
        {
            return args[index + 1];
        }

        return null;
    }

    public static void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  DatabaseBackupUtility backup --config <path_to_config>");
        Console.WriteLine("  DatabaseBackupUtility restore --config <path_to_config>");
        Console.WriteLine("Options:");
        Console.WriteLine("  --config <path>  Specify the path to the configuration file.");
    }
}