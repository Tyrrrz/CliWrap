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
        private readonly string _echoArgsBat;
        private readonly string _echoEnvVarBat;
        private readonly string _echoStdinBat;
        private readonly string _neverEndingBat;
        private readonly string _throwErrorBat;

        public CliTests()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _echoArgsBat = Path.Combine(testDir, "Bats\\EchoArgs.bat");
            _echoEnvVarBat = Path.Combine(testDir, "Bats\\EchoEnvVar.bat");
            _echoStdinBat = Path.Combine(testDir, "Bats\\EchoStdin.bat");
            _neverEndingBat = Path.Combine(testDir, "Bats\\NeverEnding.bat");
            _throwErrorBat = Path.Combine(testDir, "Bats\\ThrowError.bat");
        }

        [Test]
        public void Execute_EchoArgs_Test()
        {
            var cli = new Cli(_echoArgsBat);

            var output = cli.Execute("Hello world");
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void Execute_EchoStdin_Test()
        {
            var cli = new Cli(_echoStdinBat);

            var input = new ExecutionInput(standardInput: "Hello world");
            var output = cli.Execute(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void Execute_EchoStdin_Empty_Test()
        {
            var cli = new Cli(_echoStdinBat);

            var output = cli.Execute();
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void Execute_EchoEnvVar_Test()
        {
            var cli = new Cli(_echoEnvVarBat);

            var input = new ExecutionInput();
            input.EnvironmentVariables.Add("TEST_ENV_VAR", "Hello world");

            var output = cli.Execute(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void Execute_ThrowError_Test()
        {
            var cli = new Cli(_throwErrorBat);

            var output = cli.Execute();
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(output.StandardError.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void Execute_NeverEnding_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_neverEndingBat);

                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() => cli.Execute(cts.Token));
            }
        }

        [Test]
        public void Execute_NeverEnding_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_neverEndingBat);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.Throws<OperationCanceledException>(() => cli.Execute(cts.Token));
            }
        }

        [Test]
        public void ExecuteAndForget_EchoArgs_Test()
        {
            var cli = new Cli(_echoArgsBat);

            cli.ExecuteAndForget("Hello world");
        }

        [Test]
        public async Task ExecuteAsync_EchoArgs_Test()
        {
            var cli = new Cli(_echoArgsBat);

            var output = await cli.ExecuteAsync("Hello world");
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdin_Test()
        {
            var cli = new Cli(_echoStdinBat);

            var input = new ExecutionInput(standardInput: "Hello world");
            var output = await cli.ExecuteAsync(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdin_Empty_Test()
        {
            var cli = new Cli(_echoStdinBat);

            var output = await cli.ExecuteAsync();
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ExecuteAsync_EchoEnvVar_Test()
        {
            var cli = new Cli(_echoEnvVarBat);

            var input = new ExecutionInput();
            input.EnvironmentVariables.Add("TEST_ENV_VAR", "Hello world");

            var output = await cli.ExecuteAsync(input);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task ExecuteAsync_ThrowError_Test()
        {
            var cli = new Cli(_throwErrorBat);

            var output = await cli.ExecuteAsync();
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(14));
            Assert.That(output.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(output.StandardError.TrimEnd(), Is.EqualTo("Hello world"));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThan(output.ExitTime));
            Assert.That(output.RunTime, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void ExecuteAsync_NeverEnding_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_neverEndingBat);

                cts.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
            }
        }

        [Test]
        public void ExecuteAsync_NeverEnding_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_neverEndingBat);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
            }
        }

        [Test]
        public void KillAllProcesses_AfterExecute_NeverEnding_Test()
        {
            var cli = new Cli(_neverEndingBat);

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
        public async Task KillAllProcesses_AfterExecuteAsync_NeverEnding_Test()
        {
            var cli = new Cli(_neverEndingBat);

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