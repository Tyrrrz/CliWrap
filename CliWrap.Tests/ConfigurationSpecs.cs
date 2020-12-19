using System;
using System.Collections.Generic;
using System.IO;
using CliWrap.Builders;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class ConfigurationSpecs
    {
        [Fact]
        public void Command_line_arguments_can_be_set()
        {
            // Arrange
            var cmd = Cli.Wrap("foo").WithArguments("xxx");

            // Act
            var cmdOther = cmd.WithArguments("new");

            // Assert
            cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Arguments));
            cmd.Arguments.Should().NotBe(cmdOther.Arguments);
            cmdOther.Arguments.Should().Be("new");
        }

        [Fact]
        public void Command_line_arguments_can_be_set_from_a_list()
        {
            // Arrange
            var cmd = Cli.Wrap("foo").WithArguments("xxx");

            // Act
            var cmdOther = cmd.WithArguments(new[] {"-a", "foo bar"});

            // Assert
            cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Arguments));
            cmd.Arguments.Should().NotBe(cmdOther.Arguments);
            cmdOther.Arguments.Should().Be("-a \"foo bar\"");
        }

        [Fact]
        public void Command_line_arguments_can_be_set_with_a_builder()
        {
            // Arrange
            var builder = new ArgumentsBuilder();

            // Act
            var arguments = builder
                .Add("hello world")
                .Add("foo")
                .Add(1234)
                .Add(3.14)
                .Add(TimeSpan.FromMinutes(1))
                .Add(new IFormattable[] {-5, 89.13, 100.50M})
                .Add("bar")
                .Build();

            // Assert
            arguments.Should().Be("\"hello world\" foo 1234 3.14 00:01:00 -5 89.13 100.50 bar");
        }

        [Fact]
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

        [Fact]
        public void Credentials_can_be_set()
        {
            // Arrange
            var cmd = Cli.Wrap("foo").WithCredentials(c => c
                .SetDomain("xxx")
                .SetUserName("xxx")
                .SetPassword("xxx")
            );

            // Act
            var cmdOther = cmd.WithCredentials(c => c
                .SetDomain("new")
                .SetUserName("new")
                .SetPassword("new")
            );

            // Assert
            cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.Credentials));
            cmd.Credentials.Should().NotBe(cmdOther.Credentials);
            cmdOther.Credentials.Should().BeEquivalentTo(new Credentials("new", "new", "new"));
        }

        [Fact]
        public void Credentials_can_be_set_with_a_builder()
        {
            // Arrange
            var builder = new CredentialsBuilder();

            // Act
            var credentials = builder
                .SetDomain("domain")
                .SetUserName("username")
                .SetPassword("password")
                .Build();

            // Assert
            credentials.Should().BeEquivalentTo(new Credentials("domain", "username", "password"));
        }

        [Fact]
        public void Environment_variables_can_be_set()
        {
            // Arrange
            var cmd = Cli.Wrap("foo").WithEnvironmentVariables(e => e.Set("xxx", "xxx"));

            // Act
            var cmdOther = cmd.WithEnvironmentVariables(e => e.Set("new", "new"));

            // Assert
            cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.EnvironmentVariables));
            cmd.EnvironmentVariables.Should().NotBeEquivalentTo(cmdOther.EnvironmentVariables);
            cmdOther.EnvironmentVariables.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["new"] = "new"
            });
        }

        [Fact]
        public void Environment_variables_can_be_set_with_a_builder()
        {
            // Arrange
            var builder = new EnvironmentVariablesBuilder();

            // Act
            var envVars = builder
                .Set("foo", "bar")
                .Set("lo", "dash")
                .Build();

            // Assert
            envVars.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                ["foo"] = "bar",
                ["lo"] = "dash"
            });
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
}