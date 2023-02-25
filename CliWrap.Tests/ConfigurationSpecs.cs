using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests;

public class ConfigurationSpecs
{
    [Fact(Timeout = 15000)]
    public void I_can_create_a_command_with_the_default_configuration()
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
    public void I_can_configure_the_target_file()
    {
        // Arrange
        var original = Cli.Wrap("foo");

        // Act
        var modified = original.WithTargetFile("bar");

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.TargetFilePath));
        original.TargetFilePath.Should().NotBe(modified.TargetFilePath);
        modified.TargetFilePath.Should().Be("bar");
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_command_line_arguments()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithArguments("xxx");

        // Act
        var modified = original.WithArguments("qqq ppp");

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.Arguments));
        original.Arguments.Should().NotBe(modified.Arguments);
        modified.Arguments.Should().Be("qqq ppp");
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_command_line_arguments_by_passing_an_array()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithArguments("xxx");

        // Act
        var modified = original.WithArguments(new[] { "-a", "foo bar" });

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.Arguments));
        original.Arguments.Should().NotBe(modified.Arguments);
        modified.Arguments.Should().Be("-a \"foo bar\"");
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_command_line_arguments_using_a_builder()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithArguments("xxx");

        // Act
        var modified = original.WithArguments(b => b
            .Add("-a")
            .Add("foo bar")
            .Add("\"foo\\\\bar\"")
            .Add(3.14)
            .Add(new[] { "foo", "bar" })
            .Add(new IFormattable[] { -5, 89.13 })
        );

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.Arguments));
        original.Arguments.Should().NotBe(modified.Arguments);
        modified.Arguments.Should().Be("-a \"foo bar\" \"\\\"foo\\\\bar\\\"\" 3.14 foo bar -5 89.13");
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_working_directory()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithWorkingDirectory("xxx");

        // Act
        var modified = original.WithWorkingDirectory("new");

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.WorkingDirPath));
        original.WorkingDirPath.Should().NotBe(modified.WorkingDirPath);
        modified.WorkingDirPath.Should().Be("new");
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_user_credentials()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithCredentials(new Credentials("xxx", "xxx", "xxx"));

        // Act
        var modified = original.WithCredentials(new Credentials("domain", "username", "password", true));

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.Credentials));
        original.Credentials.Should().NotBe(modified.Credentials);
        modified.Credentials.Should().BeEquivalentTo(new Credentials("domain", "username", "password", true));
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_user_credentials_using_a_builder()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithCredentials(new Credentials("xxx", "xxx", "xxx"));

        // Act
        var modified = original.WithCredentials(c => c
            .SetDomain("domain")
            .SetUserName("username")
            .SetPassword("password")
            .LoadUserProfile()
        );

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.Credentials));
        original.Credentials.Should().NotBe(modified.Credentials);
        modified.Credentials.Should().BeEquivalentTo(new Credentials("domain", "username", "password", true));
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_environment_variables()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithEnvironmentVariables(e => e.Set("xxx", "xxx"));

        // Act
        var modified = original.WithEnvironmentVariables(new Dictionary<string, string?>
        {
            ["name"] = "value",
            ["key"] = "door"
        });

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.EnvironmentVariables));
        original.EnvironmentVariables.Should().NotBeEquivalentTo(modified.EnvironmentVariables);
        modified.EnvironmentVariables.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["name"] = "value",
            ["key"] = "door"
        });
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_environment_variables_using_a_builder()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithEnvironmentVariables(e => e.Set("xxx", "xxx"));

        // Act
        var modified = original.WithEnvironmentVariables(b => b
            .Set("name", "value")
            .Set("key", "door")
            .Set(new Dictionary<string, string?>
            {
                ["zzz"] = "yyy",
                ["aaa"] = "bbb"
            })
        );

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.EnvironmentVariables));
        original.EnvironmentVariables.Should().NotBeEquivalentTo(modified.EnvironmentVariables);
        modified.EnvironmentVariables.Should().BeEquivalentTo(new Dictionary<string, string?>
        {
            ["name"] = "value",
            ["key"] = "door",
            ["zzz"] = "yyy",
            ["aaa"] = "bbb"
        });
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_result_validation_strategy()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithValidation(CommandResultValidation.ZeroExitCode);

        // Act
        var modified = original.WithValidation(CommandResultValidation.None);

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.Validation));
        original.Validation.Should().NotBe(modified.Validation);
        modified.Validation.Should().Be(CommandResultValidation.None);
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_stdin_pipe()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithStandardInputPipe(PipeSource.Null);

        // Act
        var modified = original.WithStandardInputPipe(PipeSource.FromString("new"));

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.StandardInputPipe));
        original.StandardInputPipe.Should().NotBeSameAs(modified.StandardInputPipe);
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_stdout_pipe()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithStandardOutputPipe(PipeTarget.Null);

        // Act
        var modified = original.WithStandardOutputPipe(PipeTarget.ToStream(Stream.Null));

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.StandardOutputPipe));
        original.StandardOutputPipe.Should().NotBeSameAs(modified.StandardOutputPipe);
    }

    [Fact(Timeout = 15000)]
    public void I_can_configure_the_stderr_pipe()
    {
        // Arrange
        var original = Cli.Wrap("foo").WithStandardErrorPipe(PipeTarget.Null);

        // Act
        var modified = original.WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));

        // Assert
        original.Should().BeEquivalentTo(modified, o => o.Excluding(c => c.StandardErrorPipe));
        original.StandardErrorPipe.Should().NotBeSameAs(modified.StandardErrorPipe);
    }
}