using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class SqlDumpTableFilterTests
{
    [Fact]
    public void FilterByTables_KeepsOnlyStatementsForRequestedMySqlTable()
    {
        const string dump = """
        SET NAMES utf8;
        DROP TABLE IF EXISTS `orders`;
        CREATE TABLE `orders` (`id` int) ENGINE=InnoDB;
        INSERT INTO `orders` VALUES (1),(2);
        DROP TABLE IF EXISTS `customers`;
        CREATE TABLE `customers` (`id` int) ENGINE=InnoDB;
        INSERT INTO `customers` VALUES (10),(20);
        """;

        var filtered = SqlDumpTableFilter.FilterByTables(dump, ["orders"]);

        Assert.Contains("CREATE TABLE `orders`", filtered);
        Assert.Contains("INSERT INTO `orders`", filtered);
        Assert.DoesNotContain("customers", filtered);
        Assert.Contains("SET NAMES utf8;", filtered);
    }

    [Fact]
    public void FilterByTables_KeepsWholeCopyBlockForRequestedPostgresTable()
    {
        const string dump = """
        SET statement_timeout = 0;
        CREATE TABLE public.orders (id integer);
        COPY public.orders (id) FROM stdin;
        1
        2
        \.
        CREATE TABLE public.customers (id integer);
        COPY public.customers (id) FROM stdin;
        10
        20
        \.
        """;

        var filtered = SqlDumpTableFilter.FilterByTables(dump, ["orders"]);

        Assert.Contains("CREATE TABLE public.orders", filtered);
        Assert.Contains("COPY public.orders", filtered);
        Assert.Contains("1\n2\n\\.", filtered);
        Assert.DoesNotContain("customers", filtered);
    }

    [Fact]
    public void FilterByTables_DropsUnrelatedStatements()
    {
        const string dump = """
        CREATE TABLE public.other (id integer);
        INSERT INTO public.other VALUES (1);
        """;

        var filtered = SqlDumpTableFilter.FilterByTables(dump, ["orders"]);

        Assert.Equal(string.Empty, filtered);
    }
}
