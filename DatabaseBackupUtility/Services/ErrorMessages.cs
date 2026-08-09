namespace DatabaseBackupUtility.Services;

internal static class ErrorMessages
{
    public static string DescribeProcessFailure(string toolName, int exitCode, string errorOutput)
    {
        if (exitCode == 127 || errorOutput.Contains("command not found", StringComparison.OrdinalIgnoreCase))
            return $"'{toolName}' was not found in PATH. Install the required database client tools and make sure '{toolName}' is available.";

        if (IsAuthFailure(errorOutput))
            return $"Authentication failed while running '{toolName}': check the configured username and password.";

        if (IsUnreachable(errorOutput))
            return $"Could not reach the database server while running '{toolName}': connection refused or host unreachable.";

        return $"'{toolName}' failed (exit {exitCode}): {errorOutput}".TrimEnd();
    }

    public static string DescribeConnectionFailure(string dbLabel, string host, string message)
    {
        if (IsAuthFailure(message))
            return $"Authentication failed for {dbLabel} database at {host}: check the configured username and password.";

        if (IsUnreachable(message))
            return $"Could not reach {dbLabel} server at {host}: connection refused, timed out, or host unreachable.";

        return $"Failed to connect to {dbLabel} database at {host}: {message}";
    }

    private static bool IsAuthFailure(string message) =>
        message.Contains("access denied", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("authentication failed", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase);

    private static bool IsUnreachable(string message) =>
        message.Contains("refused", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("unable to connect", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("could not connect", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("timed out", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("no reachable servers", StringComparison.OrdinalIgnoreCase);
}
