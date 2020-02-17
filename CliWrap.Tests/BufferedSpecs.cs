﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CliWrap.Tests
{
    public class BufferedSpecs
    {
        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_execution_result()
        {
            // Arrange
            const int expectedExitCode = 13;

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.SetExitCode)
                    .AddArgument(expectedExitCode));

                c.EnableExitCodeValidation(false);
            }).Buffered();

            // Act
            var result = await cli.ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(expectedExitCode);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_stdout()
        {
            // Arrange
            const string expectedStdOut = "Hello stdout";

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdOut)
                    .AddArgument(expectedStdOut));
            }).Buffered();

            // Act
            var result = await cli.ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().Be(expectedStdOut);
            result.StandardError.TrimEnd().Should().BeEmpty();
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_stderr()
        {
            // Arrange
            const string expectedStdErr = "Hello stderr";

            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.EchoStdErr)
                    .AddArgument(expectedStdErr));
            }).Buffered();

            // Act
            var result = await cli.ExecuteAsync();

            // Assert
            result.ExitCode.Should().Be(0);
            result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
            result.StandardOutput.TrimEnd().Should().BeEmpty();
            result.StandardError.TrimEnd().Should().Be(expectedStdErr);
        }

        [Fact]
        public async Task I_can_execute_a_CLI_and_get_the_underlying_process_ID_while_it_is_running()
        {
            // Arrange
            var cli = Cli.Wrap("dotnet", Dummy.Program.Location).Buffered();

            // Act
            var task = cli.ExecuteAsync();

            // Assert
            task.ProcessId.Should().NotBe(0);
            await task;
        }

        [Fact(Timeout = 10000)]
        public async Task I_can_execute_a_CLI_and_it_will_not_deadlock_on_very_large_stdout_and_stderr()
        {
            // Arrange
            var cli = Cli.Wrap("dotnet", c =>
            {
                c.SetArguments(a => a
                    .AddArgument(Dummy.Program.Location)
                    .AddArgument(Dummy.Program.LoopBoth)
                    .AddArgument(100_000));
            }).Buffered();

            // Act
            await cli.ExecuteAsync();
        }
    }
}