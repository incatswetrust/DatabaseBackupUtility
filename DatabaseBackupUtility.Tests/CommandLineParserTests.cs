using DatabaseBackupUtility.Services;

namespace DatabaseBackupUtility.Tests;

public class CommandLineParserTests
{
    [Theory]
    [InlineData("backup")]
    [InlineData("BACKUP")]
    [InlineData("restore")]
    [InlineData("test-connection")]
    [InlineData("list")]
    public void IsValid_AcceptsKnownCommands(string command)
    {
        var parser = new CommandLineParser([command, "--config", "config.json"]);

        Assert.True(parser.IsValid());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    public void IsValid_RejectsUnknownCommands(string command)
    {
        var parser = new CommandLineParser([command]);

        Assert.False(parser.IsValid());
    }

    [Fact]
    public void IsValid_RejectsNullArgs()
    {
        var parser = new CommandLineParser(null);

        Assert.False(parser.IsValid());
    }

    [Fact]
    public void IsValid_RejectsEmptyArgs()
    {
        var parser = new CommandLineParser([]);

        Assert.False(parser.IsValid());
    }

    [Fact]
    public void GetCommand_ReturnsLowercasedFirstArgument()
    {
        var parser = new CommandLineParser(["Backup", "--config", "config.json"]);

        Assert.Equal("backup", parser.GetCommand());
    }

    [Fact]
    public void GetCommand_ReturnsEmptyStringWhenNoArgs()
    {
        var parser = new CommandLineParser([]);

        Assert.Equal(string.Empty, parser.GetCommand());
    }

    [Fact]
    public void GetCommand_ReturnsEmptyStringWhenArgsNull()
    {
        var parser = new CommandLineParser(null);

        Assert.Equal(string.Empty, parser.GetCommand());
    }

    [Fact]
    public void GetOption_ReturnsValueFollowingOption()
    {
        var parser = new CommandLineParser(["backup", "--config", "config.json"]);

        Assert.Equal("config.json", parser.GetOption("--config"));
    }

    [Fact]
    public void GetOption_ReturnsNullWhenOptionMissing()
    {
        var parser = new CommandLineParser(["backup", "--config", "config.json"]);

        Assert.Null(parser.GetOption("--file"));
    }

    [Fact]
    public void GetOption_ReturnsNullWhenOptionIsLastArgument()
    {
        var parser = new CommandLineParser(["backup", "--config"]);

        Assert.Null(parser.GetOption("--config"));
    }

    [Fact]
    public void GetOption_ReturnsNullWhenArgsNull()
    {
        var parser = new CommandLineParser(null);

        Assert.Null(parser.GetOption("--config"));
    }

    [Theory]
    [InlineData("--compress")]
    [InlineData("--dry-run")]
    public void HasFlag_ReturnsTrueWhenFlagPresent(string flag)
    {
        var parser = new CommandLineParser(["backup", "--config", "config.json", flag]);

        Assert.True(parser.HasFlag(flag));
    }

    [Fact]
    public void HasFlag_ReturnsFalseWhenFlagAbsent()
    {
        var parser = new CommandLineParser(["backup", "--config", "config.json"]);

        Assert.False(parser.HasFlag("--compress"));
    }

    [Fact]
    public void HasFlag_ReturnsFalseWhenArgsNull()
    {
        var parser = new CommandLineParser(null);

        Assert.False(parser.HasFlag("--compress"));
    }
}
