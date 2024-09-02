namespace DatabaseBackupUtility.Configs;

public class CommandLineParser
{
    private readonly string[] _args;

    public CommandLineParser(string[] args)
    {
        _args = args;
    }

    public bool IsValid()
    {
        // Проверяем, что переданы хотя бы один аргумент
        if (_args == null || _args.Length == 0)
        {
            ShowUsage();
            return false;
        }

        // Проверяем валидность команд и опций
        string command = _args[0].ToLower();
        if (command != "backup" && command != "restore")
        {
            ShowUsage();
            return false;
        }

        return true;
    }

    public string GetCommand()
    {
        return _args.Length > 0 ? _args[0].ToLower() : string.Empty;
    }

    public string GetOption(string optionName)
    {
        // Ищем опцию в аргументах
        int index = Array.IndexOf(_args, optionName);
        if (index >= 0 && index < _args.Length - 1)
        {
            return _args[index + 1];
        }
        return null;
    }

    public void ShowUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  DatabaseBackupUtility backup --config <path_to_config>");
        Console.WriteLine("  DatabaseBackupUtility restore --config <path_to_config>");
        Console.WriteLine("Options:");
        Console.WriteLine("  --config <path>  Specify the path to the configuration file.");
    }
}