using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Models;
using NUnit.Framework;

namespace CliWrap.Tests
{
    [TestFixture]
    [Timeout(5000)]
    public class CliTests
    {
        private const string TestString = "Hello world";
        private const string TestEnvVar = "TEST_ENV_VAR";

        private readonly string _echoArgsToStdoutBat;
        private readonly string _echoStdinToStdoutBat;
        private readonly string _echoEnvVarToStdoutBat;
        private readonly string _echoArgsToStderrBat;
        private readonly string _echoArgsToStdoutLoopBat;

        public CliTests()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _echoArgsToStdoutBat = Path.Combine(testDir, "Bats\\EchoArgsToStdout.bat");
            _echoStdinToStdoutBat = Path.Combine(testDir, "Bats\\EchoStdinToStdout.bat");
            _echoEnvVarToStdoutBat = Path.Combine(testDir, "Bats\\EchoEnvVarToStdout.bat");
            _echoArgsToStderrBat = Path.Combine(testDir, "Bats\\EchoArgsToStderr.bat");
            _echoArgsToStdoutLoopBat = Path.Combine(testDir, "Bats\\EchoArgsToStdoutLoop.bat");
        }

        [Test]
        public void Execute_EchoArgsToStdout_Test()
        {
            var cli = new Cli(_echoArgsToStdoutBat);

            var output = cli.Execute(TestString);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Test()
        {
            var cli = new Cli(_echoStdinToStdoutBat);

            var input = new ExecutionInput(standardInput: TestString);
            var output = cli.Execute(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Empty_Test()
        {
            var cli = new Cli(_echoStdinToStdoutBat);

            var output = cli.Execute();
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoEnvVarToStdout_Test()
        {
            var cli = new Cli(_echoEnvVarToStdoutBat);

            var input = new ExecutionInput();
            input.EnvironmentVariables.Add(TestEnvVar, TestString);

            var output = cli.Execute(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoArgsToStderr_Test()
        {
            var cli = new Cli(_echoArgsToStderrBat);

            var output = cli.Execute(TestString);
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(output.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoArgsToStdoutLoop_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_echoArgsToStdoutLoopBat);

                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() => cli.Execute(TestString, cts.Token));
            }
        }

        [Test]
        public void Execute_EchoArgsToStdoutLoop_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_echoArgsToStdoutLoopBat);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.Throws<OperationCanceledException>(() => cli.Execute(TestString, cts.Token));
            }
        }

        [Test]
        public void ExecuteAndForget_EchoArgsToStdout_Test()
        {
            var cli = new Cli(_echoArgsToStdoutBat);

            cli.ExecuteAndForget(TestString);
        }

        [Test]
        public async Task ExecuteAsync_EchoArgsToStdout_Test()
        {
            var cli = new Cli(_echoArgsToStdoutBat);

            var output = await cli.ExecuteAsync(TestString);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Test()
        {
            var cli = new Cli(_echoStdinToStdoutBat);

            var input = new ExecutionInput(standardInput: TestString);
            var output = await cli.ExecuteAsync(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Empty_Test()
        {
            var cli = new Cli(_echoStdinToStdoutBat);

            var output = await cli.ExecuteAsync();
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoEnvVarToStdout_Test()
        {
            var cli = new Cli(_echoEnvVarToStdoutBat);

            var input = new ExecutionInput();
            input.EnvironmentVariables.Add(TestEnvVar, TestString);

            var output = await cli.ExecuteAsync(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoArgsToStderr_Test()
        {
            var cli = new Cli(_echoArgsToStderrBat);

            var output = await cli.ExecuteAsync(TestString);
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(output.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void ExecuteAsync_EchoArgsToStdoutLoop_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_echoArgsToStdoutLoopBat);

                cts.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync(TestString, cts.Token));
            }
        }

        [Test]
        public void ExecuteAsync_EchoArgsToStdoutLoop_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_echoArgsToStdoutLoopBat);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync(TestString, cts.Token));
            }
        }

        [Test]
        public void KillAllProcesses_AfterExecute_EchoArgsToStdoutLoop_Test()
        {
            var cli = new Cli(_echoArgsToStdoutLoopBat);

            // Kill after some time
            Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    cli.KillAllProcesses();
                })
                .Forget();

            // Execute
            var output = cli.Execute();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.Not.EqualTo(14));
        }

        [Test]
        public async Task KillAllProcesses_AfterExecuteAsync_EchoArgsToStdoutLoop_Test()
        {
            var cli = new Cli(_echoArgsToStdoutLoopBat);

            // Kill after some time
            Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    cli.KillAllProcesses();
                })
                .Forget();

            // Execute
            var output = await cli.ExecuteAsync();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.Not.EqualTo(14));
        }
    }
}