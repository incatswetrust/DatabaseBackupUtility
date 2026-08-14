using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class Wal2JsonTranslatorTests
{
    [Fact]
    public void ToSqlStatements_TranslatesInsert()
    {
        const string payload = """
        {"change":[{"kind":"insert","schema":"public","table":"items","columnnames":["id","name","active"],"columnvalues":[1,"widget",true]}]}
        """;

        var statements = Wal2JsonTranslator.ToSqlStatements(payload).ToList();

        Assert.Equal(["INSERT INTO \"public\".\"items\" (\"id\", \"name\", \"active\") VALUES (1, 'widget', TRUE);"], statements);
    }

    [Fact]
    public void ToSqlStatements_TranslatesUpdateUsingOldKeysForWhereClause()
    {
        const string payload = """
        {"change":[{"kind":"update","schema":"public","table":"items","columnnames":["id","name"],"columnvalues":[1,"renamed"],"oldkeys":{"keynames":["id"],"keyvalues":[1]}}]}
        """;

        var statements = Wal2JsonTranslator.ToSqlStatements(payload).ToList();

        Assert.Equal(["UPDATE \"public\".\"items\" SET \"id\" = 1, \"name\" = 'renamed' WHERE \"id\" = 1;"], statements);
    }

    [Fact]
    public void ToSqlStatements_TranslatesDelete()
    {
        const string payload = """
        {"change":[{"kind":"delete","schema":"public","table":"items","oldkeys":{"keynames":["id"],"keyvalues":[7]}}]}
        """;

        var statements = Wal2JsonTranslator.ToSqlStatements(payload).ToList();

        Assert.Equal(["DELETE FROM \"public\".\"items\" WHERE \"id\" = 7;"], statements);
    }

    [Fact]
    public void ToSqlStatements_EscapesSingleQuotesInTextValues()
    {
        const string payload = """
        {"change":[{"kind":"insert","schema":"public","table":"items","columnnames":["name"],"columnvalues":["O'Brien"]}]}
        """;

        var statements = Wal2JsonTranslator.ToSqlStatements(payload).ToList();

        Assert.Equal(["INSERT INTO \"public\".\"items\" (\"name\") VALUES ('O''Brien');"], statements);
    }

    [Fact]
    public void ToSqlStatements_TranslatesNullValue()
    {
        const string payload = """
        {"change":[{"kind":"insert","schema":"public","table":"items","columnnames":["name"],"columnvalues":[null]}]}
        """;

        var statements = Wal2JsonTranslator.ToSqlStatements(payload).ToList();

        Assert.Equal(["INSERT INTO \"public\".\"items\" (\"name\") VALUES (NULL);"], statements);
    }

    [Fact]
    public void ToSqlStatements_ReturnsNothingWhenChangeArrayIsAbsent()
    {
        var statements = Wal2JsonTranslator.ToSqlStatements("{}").ToList();

        Assert.Empty(statements);
    }

    [Fact]
    public void ToSqlStatements_ThrowsForUpdateWithoutOldKeys()
    {
        const string payload = """
        {"change":[{"kind":"update","schema":"public","table":"items","columnnames":["name"],"columnvalues":["x"]}]}
        """;

        Assert.Throws<InvalidOperationException>(() => Wal2JsonTranslator.ToSqlStatements(payload).ToList());
    }
}
