using System;
using System.Collections.Generic;
using System.IO;
using CliWrap.Builders;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class BuilderSpecs
    {
        [Fact]
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_arguments()
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
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_arguments_list()
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
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_working_directory()
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
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_environment_variables()
        {
            // Arrange
            var cmd = Cli.Wrap("foo").WithEnvironmentVariables(e => e.Set("xxx", "xxx"));

            // Act
            var cmdOther = cmd.WithEnvironmentVariables(e => e.Set("new", "new"));

            // Assert
            cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.EnvironmentVariables));
            cmd.EnvironmentVariables.Should().NotBeEquivalentTo(cmdOther.EnvironmentVariables);
            cmdOther.EnvironmentVariables.Should().BeEquivalentTo(new Dictionary<string, string> {["new"] = "new"});
        }

        [Fact]
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_validation()
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
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_stdin_pipe()
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
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_stdout_pipe()
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
        public void I_can_create_a_new_command_from_an_existing_one_by_specifying_different_stderr_pipe()
        {
            // Arrange
            var cmd = Cli.Wrap("foo").WithStandardErrorPipe(PipeTarget.Null);

            // Act
            var cmdOther = cmd.WithStandardErrorPipe(PipeTarget.ToStream(Stream.Null));

            // Assert
            cmd.Should().BeEquivalentTo(cmdOther, o => o.Excluding(c => c.StandardErrorPipe));
            cmd.StandardErrorPipe.Should().NotBeSameAs(cmdOther.StandardErrorPipe);
        }

        [Fact]
        public void I_can_build_an_argument_string_from_multiple_strings()
        {
            // Arrange
            var builder = new ArgumentsBuilder();

            // Act
            var arguments = builder
                .Add("foo")
                .Add("bar")
                .Add("two words")
                .Add(new[] {"array", "of", "many"})
                .Add("quote \" in the \\\" middle")
                .Add("\\")
                .Build();

            // Assert
            arguments.Should().Be("foo bar \"two words\" array of many \"quote \\\" in the \\\\\\\" middle\" \\");
        }

        [Fact]
        public void I_can_build_an_argument_string_from_multiple_formattable_values()
        {
            // Arrange
            var builder = new ArgumentsBuilder();

            // Act
            var arguments = builder
                .Add("foo")
                .Add(1234)
                .Add(3.14)
                .Add(TimeSpan.FromMinutes(1))
                .Add(new IFormattable[] {-5, 89.13, 100.50M})
                .Add("bar")
                .Build();

            // Assert
            arguments.Should().Be("foo 1234 3.14 00:01:00 -5 89.13 100.50 bar");
        }

        [Fact]
        public void I_can_build_environment_variables_from_multiple_individual_variables()
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
    }
}