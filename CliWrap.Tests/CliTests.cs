using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Exceptions;
using CliWrap.Models;
using CliWrap.Services;
using NUnit.Framework;

namespace CliWrap.Tests
{
    [TestFixture]
    [Timeout(5000)]
    public class CliTests
    {
        private const string TestString = "Hello world";
        private const int TestExitCode = 14;
        private const string TestEnvVar = "TEST_ENV_VAR";

        private readonly string _echoArgsToStdoutBat;
        private readonly string _echoStdinToStdoutBat;
        private readonly string _echoEnvVarToStdoutBat;
        private readonly string _echoArgsToStderrBat;
        private readonly string _sleepBat;
        private readonly string _echoSpamBat;

        public CliTests()
        {
            var testDir = TestContext.CurrentContext.TestDirectory;
            _echoArgsToStdoutBat = Path.Combine(testDir, "Bats\\EchoArgsToStdout.bat");
            _echoStdinToStdoutBat = Path.Combine(testDir, "Bats\\EchoStdinToStdout.bat");
            _echoEnvVarToStdoutBat = Path.Combine(testDir, "Bats\\EchoEnvVarToStdout.bat");
            _echoArgsToStderrBat = Path.Combine(testDir, "Bats\\EchoArgsToStderr.bat");
            _sleepBat = Path.Combine(testDir, "Bats\\Sleep.bat");
            _echoSpamBat = Path.Combine(testDir, "Bats\\EchoSpam.bat");
        }

        #region Execute

        [Test]
        public void Execute_EchoArgsToStdout_Test()
        {
            var cli = new Cli(_echoArgsToStdoutBat);

            var output = cli.Execute(TestString);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
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
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoStdinToStdout_Empty_Test()
        {
            var cli = new Cli(_echoStdinToStdoutBat);

            var output = cli.Execute();
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
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
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_EchoArgsToStderr_Test()
        {
            var cli = new Cli(_echoArgsToStderrBat);

            var output = cli.Execute(TestString);
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(output.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void Execute_Sleep_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat);

                cts.Cancel();

                Assert.Throws<OperationCanceledException>(() => cli.Execute(cts.Token));
            }
        }

        [Test]
        public void Execute_Sleep_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.Throws<OperationCanceledException>(() => cli.Execute(cts.Token));
            }
        }

        [Test]
        public void Execute_EchoSpam_BufferHandler_Test()
        {
            var cli = new Cli(_echoSpamBat);

            // Collect stdout/stderr from handler separately
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            var handler = new BufferHandler(
                stdOutLine => stdOutBuffer.AppendLine(stdOutLine),
                stdErrLine => stdErrBuffer.AppendLine(stdErrLine));

            var output = cli.Execute(bufferHandler: handler);
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput, Is.EqualTo(stdOutBuffer.ToString()));
            Assert.That(output.StandardError, Is.EqualTo(stdErrBuffer.ToString()));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        #endregion

        #region ExecuteAndForget

        [Test]
        public void ExecuteAndForget_EchoArgsToStdout_Test()
        {
            var cli = new Cli(_echoArgsToStdoutBat);

            cli.ExecuteAndForget(TestString);
        }

        #endregion

        #region ExecuteAsync

        [Test]
        public async Task ExecuteAsync_EchoArgsToStdout_Test()
        {
            var cli = new Cli(_echoArgsToStdoutBat);

            var output = await cli.ExecuteAsync(TestString);
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
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
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoStdinToStdout_Empty_Test()
        {
            var cli = new Cli(_echoStdinToStdoutBat);

            var output = await cli.ExecuteAsync();
            output.ThrowIfError();

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo("ECHO is off."));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
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
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError.TrimEnd(), Is.Empty);
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public async Task ExecuteAsync_EchoArgsToStderr_Test()
        {
            var cli = new Cli(_echoArgsToStderrBat);

            var output = await cli.ExecuteAsync(TestString);
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput.TrimEnd(), Is.Empty);
            Assert.That(output.StandardError.TrimEnd(), Is.EqualTo(TestString));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelEarly_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat);

                cts.Cancel();

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
            }
        }

        [Test]
        public void ExecuteAsync_Sleep_CancelLate_Test()
        {
            using (var cts = new CancellationTokenSource())
            {
                var cli = new Cli(_sleepBat);

                cts.CancelAfter(TimeSpan.FromSeconds(1));

                Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync(cts.Token));
            }
        }

        [Test]
        public async Task ExecuteAsync_EchoSpam_BufferHandler_Test()
        {
            var cli = new Cli(_echoSpamBat);

            // Collect stdout/stderr from handler separately
            var stdOutBuffer = new StringBuilder();
            var stdErrBuffer = new StringBuilder();
            var handler = new BufferHandler(
                stdOutLine => stdOutBuffer.AppendLine(stdOutLine),
                stdErrLine => stdErrBuffer.AppendLine(stdErrLine));

            var output = await cli.ExecuteAsync(bufferHandler: handler);
            var ex = Assert.Throws<StandardErrorException>(() => output.ThrowIfError());

            Assert.That(output, Is.Not.Null);
            Assert.That(output.ExitCode, Is.EqualTo(TestExitCode));
            Assert.That(output.StandardOutput, Is.EqualTo(stdOutBuffer.ToString()));
            Assert.That(output.StandardError, Is.EqualTo(stdErrBuffer.ToString()));
            Assert.That(output.StandardError, Is.EqualTo(ex.StandardError));
            Assert.That(output.StartTime, Is.LessThanOrEqualTo(output.ExitTime));
            Assert.That(output.RunTime, Is.EqualTo(output.ExitTime - output.StartTime));
        }

        #endregion

        #region CancelAll

        [Test]
        public void CancelAll_AfterExecute_Sleep_Test()
        {
            var cli = new Cli(_sleepBat);

            // Kill after some time
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                cli.CancelAll();
            }).Forget();

            // Execute
            Assert.Throws<OperationCanceledException>(() => cli.Execute());
        }

        [Test]
        public void CancelAll_AfterExecuteAsync_Sleep_Test()
        {
            var cli = new Cli(_sleepBat);

            // Kill after some time
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                cli.CancelAll();
            }).Forget();

            // Execute
            Assert.ThrowsAsync<TaskCanceledException>(() => cli.ExecuteAsync());
        }

        #endregion
    }
}