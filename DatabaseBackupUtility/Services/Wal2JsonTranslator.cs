using System.Text.Json;

namespace DatabaseBackupUtility.Services;

// Translates a single wal2json change payload (as returned by pg_logical_slot_get_changes /
// pg_logical_slot_peek_changes with the wal2json output plugin) into replayable SQL statements.
//
// Covers the common scalar column types (numeric, boolean, text/timestamp/uuid/json-as-text,
// null); other values are serialized as quoted text, which round-trips correctly for most types
// but may need review for exotic ones (arrays, composite types, bytea).
internal static class Wal2JsonTranslator
{
    public static IEnumerable<string> ToSqlStatements(string wal2JsonPayload)
    {
        using var document = JsonDocument.Parse(wal2JsonPayload);
        if (!document.RootElement.TryGetProperty("change", out var changes))
            yield break;

        foreach (var change in changes.EnumerateArray())
        {
            var kind = change.GetProperty("kind").GetString();
            var table = QualifiedTableName(change);

            var statement = kind switch
            {
                "insert" => BuildInsert(table, change),
                "update" => BuildUpdate(table, change),
                "delete" => BuildDelete(table, change),
                _ => null
            };

            if (statement is not null)
                yield return statement;
        }
    }

    private static string QualifiedTableName(JsonElement change)
    {
        var schema = change.GetProperty("schema").GetString();
        var table = change.GetProperty("table").GetString();
        return $"\"{schema}\".\"{table}\"";
    }

    private static string BuildInsert(string table, JsonElement change)
    {
        var columns = change.GetProperty("columnnames").EnumerateArray().Select(c => c.GetString()).ToList();
        var values = change.GetProperty("columnvalues").EnumerateArray().Select(FormatValue).ToList();
        var columnList = string.Join(", ", columns.Select(c => $"\"{c}\""));
        var valueList = string.Join(", ", values);
        return $"INSERT INTO {table} ({columnList}) VALUES ({valueList});";
    }

    private static string BuildUpdate(string table, JsonElement change)
    {
        var columns = change.GetProperty("columnnames").EnumerateArray().Select(c => c.GetString()).ToList();
        var values = change.GetProperty("columnvalues").EnumerateArray().Select(FormatValue).ToList();
        var setClause = string.Join(", ", columns.Zip(values, (c, v) => $"\"{c}\" = {v}"));
        return $"UPDATE {table} SET {setClause} WHERE {BuildWhereClause(change)};";
    }

    private static string BuildDelete(string table, JsonElement change) =>
        $"DELETE FROM {table} WHERE {BuildWhereClause(change)};";

    private static string BuildWhereClause(JsonElement change)
    {
        if (!change.TryGetProperty("oldkeys", out var oldKeys))
            throw new InvalidOperationException(
                "wal2json did not include row identity ('oldkeys') for an update/delete. " +
                "Set REPLICA IDENTITY FULL (or ensure the table has a primary key) on the affected table.");

        var names = oldKeys.GetProperty("keynames").EnumerateArray().Select(c => c.GetString()).ToList();
        var values = oldKeys.GetProperty("keyvalues").EnumerateArray().Select(FormatValue).ToList();
        return string.Join(" AND ", names.Zip(values, (n, v) => $"\"{n}\" = {v}"));
    }

    private static string FormatValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => "NULL",
        JsonValueKind.True => "TRUE",
        JsonValueKind.False => "FALSE",
        JsonValueKind.Number => value.GetRawText(),
        _ => $"'{value.ToString().Replace("'", "''")}'"
    };
}
