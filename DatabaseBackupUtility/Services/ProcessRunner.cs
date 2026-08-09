using System.Diagnostics;
using System.Text;

namespace DatabaseBackupUtility.Services;

internal static class ProcessRunner
{
    public static async Task RunAsync(string toolName, string command, CancellationToken cancellationToken,
        IDictionary<string, string>? environmentVariables = null)
    {
        var processInfo = new ProcessStartInfo("bash", $"-c \"{command}\"")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (environmentVariables is not null)
        {
            foreach (var (key, value) in environmentVariables)
                processInfo.Environment[key] = value;
        }

        using var process = new Process { StartInfo = processInfo, EnableRaisingEvents = true };
        var errorOutput = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                Console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            errorOutput.AppendLine(e.Data);
            Console.WriteLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"'{toolName}' failed (exit {process.ExitCode}): {errorOutput}".TrimEnd());
    }
}
