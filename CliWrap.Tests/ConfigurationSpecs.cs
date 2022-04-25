using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ConfigurationSpecs
{
    [Fact(Timeout = 15000)]
    public void Command_is_created_with_default_configuration()
    {
        // Act
        var cmd = Cli.Wrap("foo");

        // Assert
        cmd.TargetFilePath.Should().Be("foo");
        cmd.Arguments.Should().BeEmpty();
        cmd.WorkingDirPath.Should().Be(Directory.GetCurrentDirectory());
        cmd.Credentials.Should().BeEquivalentTo(Credentials.Default);
        cmd.EnvironmentVariables.Should().BeEmpty();
        cmd.Validation.Should().Be(CommandResultValidation.ZeroExitCode);
        cmd.StandardInputPipe.Should().Be(PipeSource.Null);
        cmd.StandardOutputPipe.Should().Be(PipeTarget.Null);
        cmd.StandardErrorPipe.Should().Be(PipeTarget.Null);
    }

    [Fact(Timeout = 15000)]
    public void Command_line_arguments_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithArguments("xxx");

        // Act
        var cmdOther = cmd.WithArguments("qqq ppp");

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Arguments));
        cmd.Arguments.Should().NotBe(cmdOther.Arguments);
        cmdOther.Arguments.Should().Be("qqq ppp");
    }

    [Fact(Timeout = 15000)]
    public void Command_line_arguments_can_be_set_from_a_list()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithArguments("xxx");

        // Act
        var cmdOther = cmd.WithArguments(new[] { "-a", "foo bar" });

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Arguments));
        cmd.Arguments.Should().NotBe(cmdOther.Arguments);
        cmdOther.Arguments.Should().Be("-a \"foo bar\"");
    }

    [Fact(Timeout = 15000)]
    public void Command_line_arguments_can_be_set_using_a_builder()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithArguments("xxx");

        // Act
        var cmdOther = cmd.WithArguments(b => b
            .Add("-a")
            .Add("foo bar")
            .Add("\"foo\\\\bar\"")
            .Add(3.14)
            .Add(new[] { "foo", "bar" })
            .Add(new IFormattable[] { -5, 89.13 })
        );

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Arguments));
        cmd.Arguments.Should().NotBe(cmdOther.Arguments);
        cmdOther.Arguments.Should().Be("-a \"foo bar\" \"\\\"foo\\\\bar\\\"\" 3.14 foo bar -5 89.13");
    }

    [Fact(Timeout = 15000)]
    public void Working_directory_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithWorkingDirectory("xxx");

        // Act
        var cmdOther = cmd.WithWorkingDirectory("new");

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.WorkingDirPath));
        cmd.WorkingDirPath.Should().NotBe(cmdOther.WorkingDirPath);
        cmdOther.WorkingDirPath.Should().Be("new");
    }

    [Fact(Timeout = 15000)]
    public void Credentials_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithCredentials(new Credentials("xxx", "xxx", "xxx"));

        // Act
        var cmdOther = cmd.WithCredentials(new Credentials("domain", "username", "password"));

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Credentials));
        cmd.Credentials.Should().NotBe(cmdOther.Credentials);
        cmdOther.Credentials.Should().BeEquivalentTo(new Credentials("domain", "username", "password"));
    }

    [Fact(Timeout = 15000)]
    public void Credentials_can_be_set_with_a_builder()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithCredentials(new Credentials("xxx", "xxx", "xxx"));

        // Act
        var cmdOther = cmd.WithCredentials(c => c
            .SetDomain("domain")
            .SetUserName("username")
            .SetPassword("password")
        );

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Credentials));
        cmd.Credentials.Should().NotBe(cmdOther.Credentials);
        cmdOther.Credentials.Should().BeEquivalentTo(new Credentials("domain", "username", "password"));
    }

    [Fact(Timeout = 15000)]
    public void Environment_variables_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithEnvironmentVariables(e => e.Set("xxx", "xxx"));

        // Act
        var cmdOther = cmd.WithEnvironmentVariables(new Dictionary<string, string?>
        {
            ["name"] = "value",
            ["key"] = "door"
        });

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.EnvironmentVariables));
        cmd.EnvironmentVariables.Should().NotBeEquivalentTo(cmdOther.EnvironmentVariables);
        cmdOther.EnvironmentVariables.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["name"] = "value",
            ["key"] = "door"
        });
    }

    [Fact(Timeout = 15000)]
    public void Environment_variables_can_be_set_with_a_builder()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithEnvironmentVariables(e => e.Set("xxx", "xxx"));

        // Act
        var cmdOther = cmd.WithEnvironmentVariables(b => b
            .Set("name", "value")
            .Set("key", "door")
            .Set(new Dictionary<string, string?>
            {
                ["zzz"] = "yyy",
                ["aaa"] = "bbb"
            })
        );

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.EnvironmentVariables));
        cmd.EnvironmentVariables.Should().NotBeEquivalentTo(cmdOther.EnvironmentVariables);
        cmdOther.EnvironmentVariables.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["name"] = "value",
            ["key"] = "door",
            ["zzz"] = "yyy",
            ["aaa"] = "bbb"
        });
    }

    [Fact(Timeout = 15000)]
    public void Result_validation_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithValidation(CommandResultValidation.ZeroExitCode);

        // Act
        var cmdOther = cmd.WithValidation(CommandResultValidation.None);

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Validation));
        cmd.Validation.Should().NotBe(cmdOther.Validation);
        cmdOther.Validation.Should().Be(CommandResultValidation.None);
    }

    [Fact(Timeout = 15000)]
    public void Stdin_pipe_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithStandardInputPipe(PipeSource.Null);

        // Act
        var cmdOther = cmd.WithStandardInputPipe(PipeSource.FromString("new"));

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.StandardInputPipe));
        cmd.StandardInputPipe.Should().NotBeSameAs(cmdOther.StandardInputPipe);
    }

    [Fact(Timeout = 15000)]
    public void Stdout_pipe_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithStandardOutputPipe(PipeTarget.Null);

        // Act
        var cmdOther = cmd.WithStandardOutputPipe(PipeTarget.ToStream(Stream.Null));

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.StandardOutputPipe));
        cmd.StandardOutputPipe.Should().NotBeSameAs(cmdOther.StandardOutputPipe);
    }

    [Fact(Timeout = 15000)]
    public void Stderr_pipe_can_be_set()
    {
        // Arrange
        var cmd = Cli.Wrap("foo").WithStandardErrorPipe(PipeTarget.Null);

        // Act
        var cmdOther = cmd.WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));

        // Assert
        cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.StandardErrorPipe));
        cmd.StandardErrorPipe.Should().NotBeSameAs(cmdOther.StandardErrorPipe);
    }
}