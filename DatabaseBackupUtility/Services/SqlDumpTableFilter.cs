using System.Text.RegularExpressions;

namespace DatabaseBackupUtility.Services;

// Best-effort, line-oriented filter that keeps only the statements belonging to specific tables
// from a plain-SQL dump (mysqldump or `pg_dump`'s default plain-text format), for selective
// restore. Session-wide setup statements (SET, versioned comments, transaction control) are
// always kept. `COPY ... FROM stdin` blocks (pg_dump's default data format) are treated as a
// single unit up to the terminating "\.", since their data rows don't otherwise mention the
// table name.
//
// This is not a full SQL parser: a semicolon inside a multi-line string literal, or a table name
// that only appears incidentally (e.g. as a foreign key reference inside another table's
// CREATE TABLE), can throw off matching. It's intended for the common case of restoring one
// table/collection worth of data from an otherwise-full dump.
internal static class SqlDumpTableFilter
{
    public static string FilterByTables(string sqlContent, IReadOnlyList<string> tables)
    {
        var kept = SplitStatements(sqlContent).Where(statement => IsAlwaysKept(statement) || MentionsAnyTable(statement, tables));
        return string.Join('\n', kept);
    }

    private static bool IsAlwaysKept(string statement)
    {
        var trimmed = statement.TrimStart();
        return trimmed.StartsWith("SET ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("/*!", StringComparison.Ordinal)
            || trimmed.StartsWith("START TRANSACTION", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MentionsAnyTable(string statement, IReadOnlyList<string> tables) =>
        tables.Any(table => Regex.IsMatch(statement, $@"\b{Regex.Escape(table)}\b", RegexOptions.IgnoreCase));

    private static IEnumerable<string> SplitStatements(string sqlContent)
    {
        var lines = sqlContent.Replace("\r\n", "\n").Split('\n');
        var current = new List<string>();
        var inCopyBlock = false;

        foreach (var line in lines)
        {
            current.Add(line);

            if (inCopyBlock)
            {
                if (line == "\\.")
                {
                    yield return string.Join('\n', current);
                    current = [];
                    inCopyBlock = false;
                }
                continue;
            }

            if (Regex.IsMatch(line, @"^\s*COPY\s+", RegexOptions.IgnoreCase))
            {
                inCopyBlock = true;
                continue;
            }

            if (line.TrimEnd().EndsWith(';'))
            {
                yield return string.Join('\n', current);
                current = [];
            }
        }

        if (current.Any(l => l.Trim().Length > 0))
            yield return string.Join('\n', current);
    }
}
